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
    /// <summary>Aktif kayıtları filtreleyip sayfalar (önce count, sonra Skip/Take).</summary>
    Task<PagedResult<Activity>> GetPagedAsync(ActivityFilterDto filter, CancellationToken cancellationToken = default);

    Task<Activity?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> IsActiveAsync(int id, CancellationToken cancellationToken = default);

    Task AddAsync(Activity activity, CancellationToken cancellationToken = default);

    Task UpdateAsync(Activity activity, CancellationToken cancellationToken = default);
}
