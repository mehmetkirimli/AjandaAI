// Open-Meteo günlük tahmin istemcisi (anahtarsız, ADR 0025 §3). Hata durumunda istisna fırlatır;
// araç bunu modele Türkçe hata olarak döner (ASSIST-15). Özet, WMO hava kodundan Türkçe üretilir.

using System.Globalization;
using System.Text.Json;
using AjandaAI.Application.Assistant.External;

namespace AjandaAI.Infrastructure.Assistant;

internal sealed class OpenMeteoWeatherClient(HttpClient http) : IWeatherClient
{
    public async Task<IReadOnlyList<WeatherDayDto>> GetDailyForecastAsync(
        double latitude, double longitude, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        var url = string.Create(CultureInfo.InvariantCulture,
            $"v1/forecast?latitude={latitude}&longitude={longitude}" +
            $"&daily=weather_code,temperature_2m_max,temperature_2m_min,precipitation_probability_max" +
            $"&timezone=auto&start_date={from:yyyy-MM-dd}&end_date={to:yyyy-MM-dd}");
        using var response = await http.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var daily = doc.RootElement.GetProperty("daily");
        var dates = daily.GetProperty("time").EnumerateArray().ToList();
        var result = new List<WeatherDayDto>(dates.Count);
        for (var i = 0; i < dates.Count; i++)
        {
            var code = Int(daily, "weather_code", i);
            result.Add(new WeatherDayDto(
                DateOnly.Parse(dates[i].GetString()!, CultureInfo.InvariantCulture),
                Double(daily, "temperature_2m_max", i), Double(daily, "temperature_2m_min", i),
                Int(daily, "precipitation_probability_max", i), code, Describe(code)));
        }
        return result;
    }

    private static JsonElement? At(JsonElement daily, string name, int i) =>
        daily.TryGetProperty(name, out var arr) && arr.GetArrayLength() > i && arr[i].ValueKind == JsonValueKind.Number
            ? arr[i] : null;

    private static double? Double(JsonElement daily, string name, int i) => At(daily, name, i)?.GetDouble();

    private static int? Int(JsonElement daily, string name, int i) => At(daily, name, i) is { } e ? (int)e.GetDouble() : null;

    private static string Describe(int? code) => code switch
    {
        0 => "Açık",
        1 or 2 => "Parçalı bulutlu",
        3 => "Kapalı",
        45 or 48 => "Sisli",
        >= 51 and <= 57 => "Çiseleme",
        >= 61 and <= 67 => "Yağmurlu",
        >= 71 and <= 77 => "Karlı",
        >= 80 and <= 82 => "Sağanak",
        85 or 86 => "Kar sağanağı",
        >= 95 => "Gök gürültülü fırtına",
        _ => "Bilinmiyor"
    };
}
