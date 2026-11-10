// Yer arama (OpenStreetMap Nominatim; ADR 0025 §3). Uygulama saniyede en fazla 1 istek atar,
// uygulamayı tanıtan User-Agent gönderir ve sonucu önbelleğe alır (ASSIST-16). Rota/süre yoktur.

namespace AjandaAI.Application.Assistant.External;

public record PlaceDto(string Name, double Latitude, double Longitude, string? Type);

public interface IPlaceSearchClient
{
    Task<IReadOnlyList<PlaceDto>> SearchAsync(string query, CancellationToken cancellationToken = default);
}
