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
}
