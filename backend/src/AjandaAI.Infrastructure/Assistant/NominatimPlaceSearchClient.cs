// OpenStreetMap Nominatim yer arama istemcisi (ADR 0025 §3, kullanım politikası): saniyede en fazla 1 istek
// (tüm kullanıcılar için ortak sıra), uygulamayı tanıtan User-Agent ve sorgu önbelleği (ASSIST-16).
// Singleton'dır; önbellek bellek içidir ve yalnızca başarılı sonuçları tutar.

using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using AjandaAI.Application.Assistant.External;

namespace AjandaAI.Infrastructure.Assistant;

internal sealed class NominatimPlaceSearchClient(HttpClient http, TimeProvider time) : IPlaceSearchClient
{
    public static readonly TimeSpan MinInterval = TimeSpan.FromSeconds(1);

    private readonly ConcurrentDictionary<string, IReadOnlyList<PlaceDto>> _cache = new();
    private readonly SemaphoreSlim _gate = new(1, 1);
    private DateTimeOffset _lastRequest = DateTimeOffset.MinValue;

    public async Task<IReadOnlyList<PlaceDto>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        var key = query.Trim().ToLowerInvariant();
        if (_cache.TryGetValue(key, out var cached))
            return cached;

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_cache.TryGetValue(key, out cached))
                return cached;

            var wait = _lastRequest + MinInterval - time.GetUtcNow();
            if (wait > TimeSpan.Zero)
                await Task.Delay(wait, cancellationToken);
            _lastRequest = time.GetUtcNow();

            using var response = await http.GetAsync(
                $"search?q={Uri.EscapeDataString(query.Trim())}&format=jsonv2&limit=5&accept-language=tr", cancellationToken);
            response.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var places = doc.RootElement.EnumerateArray()
                .Select(e => new PlaceDto(
                    e.GetProperty("display_name").GetString() ?? string.Empty,
                    double.Parse(e.GetProperty("lat").GetString()!, CultureInfo.InvariantCulture),
                    double.Parse(e.GetProperty("lon").GetString()!, CultureInfo.InvariantCulture),
                    e.TryGetProperty("type", out var t) ? t.GetString() : null))
                .ToList();
            _cache[key] = places;
            return places;
        }
        finally
        {
            _gate.Release();
        }
    }
}
