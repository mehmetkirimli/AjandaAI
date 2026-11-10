// Nager.Date resmî tatil istemcisi (anahtarsız, ülke TR; ADR 0025 §3). Sonuç yıl bazında önbelleğe alınır;
// hata önbelleğe girmez, istisna fırlatılır ve araç modele Türkçe hata döner (ASSIST-17).

using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using AjandaAI.Application.Assistant.External;

namespace AjandaAI.Infrastructure.Assistant;

internal sealed class NagerHolidayClient(HttpClient http) : IHolidayClient
{
    public const string CountryCode = "TR";

    private readonly ConcurrentDictionary<int, IReadOnlyList<HolidayDto>> _cache = new();

    public async Task<IReadOnlyList<HolidayDto>> GetHolidaysAsync(int year, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(year, out var cached))
            return cached;

        using var response = await http.GetAsync($"api/v3/PublicHolidays/{year}/{CountryCode}", cancellationToken);
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var holidays = doc.RootElement.EnumerateArray()
            .Select(e => new HolidayDto(
                DateOnly.Parse(e.GetProperty("date").GetString()!, CultureInfo.InvariantCulture),
                e.GetProperty("localName").GetString() ?? string.Empty,
                e.GetProperty("name").GetString() ?? string.Empty))
            .ToList();
        _cache[year] = holidays;
        return holidays;
    }
}
