// Reminder kayıtlarına salt-okunur erişim sözleşmesidir.
// Implementasyonu Infrastructure/Repositories/ReminderRepository.cs içindedir.
// GetByIdAsync, bağlı Activity navigation'ını yüklenmiş olarak döner.

using AjandaAI.Domain.Entities;

namespace AjandaAI.Application.Reminders;

public interface IReminderRepository
{
    Task<IReadOnlyList<Reminder>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Reminder?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}
