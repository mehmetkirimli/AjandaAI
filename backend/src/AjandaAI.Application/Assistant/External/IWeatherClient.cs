// Hava durumu tahmini (Open-Meteo, anahtarsız; ADR 0025 §3). Uygulama Infrastructure'dadır.
// Hata durumunda istisna fırlatır; araç bunu modele Türkçe hata olarak döner (ASSIST-15).

namespace AjandaAI.Application.Assistant.External;

public record WeatherDayDto(DateOnly Date, double? MaxTemperatureC, double? MinTemperatureC,
    int? PrecipitationProbability, int? WeatherCode, string Summary);

public interface IWeatherClient
{
    Task<IReadOnlyList<WeatherDayDto>> GetDailyForecastAsync(
        double latitude, double longitude, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
}
