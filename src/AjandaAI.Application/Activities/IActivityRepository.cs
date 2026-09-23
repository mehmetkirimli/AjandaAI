// Activity kayıtlarına salt-okunur erişim sözleşmesidir.
// Implementasyonu Infrastructure/Repositories/ActivityRepository.cs içindedir.

using AjandaAI.Domain.Entities;

namespace AjandaAI.Application.Activities;

public interface IActivityRepository
{
    Task<IReadOnlyList<Activity>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Activity?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}
