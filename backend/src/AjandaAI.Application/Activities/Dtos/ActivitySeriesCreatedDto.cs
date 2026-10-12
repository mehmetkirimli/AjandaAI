// Seri oluşturma yanıtıdır: seri kimliği, oluşan aktivite sayısı, ilk ve son oluşumun başlangıcı.
// Seri deactivate yanıtı (etkilenen sayı) da burada tanımlıdır.

namespace AjandaAI.Application.Activities.Dtos;

public record ActivitySeriesCreatedDto(int SeriesId, int CreatedCount, DateTimeOffset FirstStart, DateTimeOffset LastStart);

public record SeriesDeactivatedDto(int DeactivatedCount);
