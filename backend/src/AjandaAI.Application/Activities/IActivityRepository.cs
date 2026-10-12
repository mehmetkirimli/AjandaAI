// Activity kayıtlarına okuma ve yazma erişim sözleşmesidir.
// Implementasyonu Infrastructure/Repositories/ActivityRepository.cs içindedir.
// Yazma metodları değişiklikleri kendi içinde kalıcı hale getirir (SaveChanges).
// Silme metodu yoktur: aktivite pasife alınır (IsActive = false, UpdateAsync ile).

using AjandaAI.Application.Activities.Dtos;
using AjandaAI.Application.Common;
using AjandaAI.Domain.Entities;

namespace AjandaAI.Application.Activities;

public interface IActivityRepository
{
    /// <summary>Yalnızca verilen kullanıcının aktif kayıtlarını filtreleyip sayfalar (önce count, sonra Skip/Take).</summary>
    Task<PagedResult<Activity>> GetPagedAsync(ActivityFilterDto filter, int userId, CancellationToken cancellationToken = default);

    /// <summary>Sahiplik koşulu sorgunun içindedir: WHERE id = @id AND user_id = @userId (ADR 0018).</summary>
    Task<Activity?> GetByIdForUserAsync(int id, int userId, CancellationToken cancellationToken = default);

    /// <summary>Kayıt var mı (sahibi önemsiz). Yalnızca yetkisiz erişim logu için, hata yolunda çağrılır.</summary>
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);

    Task AddAsync(Activity activity, CancellationToken cancellationToken = default);

    Task UpdateAsync(Activity activity, CancellationToken cancellationToken = default);

    /// <summary>Seriyi ve oluşumlarını (varsa hatırlatmalarıyla) tek SaveChanges / tek transaction ile kaydeder (ADR 0022).</summary>
    Task AddSeriesAsync(ActivitySeries series, IReadOnlyList<Activity> activities, CancellationToken cancellationToken = default);

    /// <summary>Sahiplik sorgunun içindedir: WHERE id = @id AND user_id = @userId.</summary>
    Task<ActivitySeries?> GetSeriesForUserAsync(int seriesId, int userId, CancellationToken cancellationToken = default);

    /// <summary>Seri var mı (sahibi önemsiz). Yalnızca yetkisiz erişim logu için.</summary>
    Task<bool> SeriesExistsAsync(int seriesId, CancellationToken cancellationToken = default);

    /// <summary>Serinin Start >= fromUtc olan aktif oluşumlarını pasife alır; etkilenen sayıyı döner.</summary>
    Task<int> DeactivateSeriesFromAsync(int seriesId, int userId, DateTimeOffset fromUtc, DateTimeOffset now, CancellationToken cancellationToken = default);
}
