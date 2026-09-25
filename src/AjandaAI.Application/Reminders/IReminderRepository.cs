// Reminder kayıtlarına okuma ve yazma erişim sözleşmesidir.
// Implementasyonu Infrastructure/Repositories/ReminderRepository.cs içindedir.
// GetByIdAsync, bağlı Activity navigation'ını yüklenmiş olarak döner.
// Yazma metodları değişiklikleri kendi içinde kalıcı hale getirir (SaveChanges).

using AjandaAI.Application.Reminders.Dtos;
using AjandaAI.Application.Common;
using AjandaAI.Domain.Entities;

namespace AjandaAI.Application.Reminders;

public interface IReminderRepository
{
    /// <summary>Kayıtları filtreleyip sayfalar (önce count, sonra Skip/Take).</summary>
    Task<PagedResult<Reminder>> GetPagedAsync(ReminderFilterDto filter, CancellationToken cancellationToken = default);

    Task<Reminder?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task AddAsync(Reminder reminder, CancellationToken cancellationToken = default);

    Task UpdateAsync(Reminder reminder, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
