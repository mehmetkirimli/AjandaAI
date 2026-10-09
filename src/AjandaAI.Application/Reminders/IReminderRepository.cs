// Reminder kayıtlarına okuma ve yazma erişim sözleşmesidir.
// Implementasyonu Infrastructure/Repositories/ReminderRepository.cs içindedir.
// Sahip, bağlı Activity'nin sahibidir (Reminder'da UserId yoktur); sahiplik koşulu sorgunun içindedir (ADR 0018).
// GetByIdForUserAsync, bağlı Activity navigation'ını yüklenmiş olarak döner.

using AjandaAI.Application.Reminders.Dtos;
using AjandaAI.Application.Common;
using AjandaAI.Domain.Entities;

namespace AjandaAI.Application.Reminders;

public interface IReminderRepository
{
    /// <summary>Yalnızca verilen kullanıcının aktivitelerine ait kayıtları filtreleyip sayfalar (önce count, sonra Skip/Take).</summary>
    Task<PagedResult<Reminder>> GetPagedAsync(ReminderFilterDto filter, int userId, CancellationToken cancellationToken = default);

    /// <summary>Sahiplik koşulu sorgunun içindedir: WHERE id = @id AND activity.user_id = @userId (ADR 0018).</summary>
    Task<Reminder?> GetByIdForUserAsync(int id, int userId, CancellationToken cancellationToken = default);

    /// <summary>Kayıt var mı (sahibi önemsiz). Yalnızca yetkisiz erişim logu için, hata yolunda çağrılır.</summary>
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);

    Task AddAsync(Reminder reminder, CancellationToken cancellationToken = default);

    Task UpdateAsync(Reminder reminder, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
