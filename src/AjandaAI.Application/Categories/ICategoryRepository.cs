// Category lookup tablosuna salt-okunur erişim sözleşmesidir.
// Implementasyonu Infrastructure/Repositories/CategoryRepository.cs içindedir.

using AjandaAI.Domain.Entities;

namespace AjandaAI.Application.Categories;

public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> GetActiveAsync(CancellationToken cancellationToken = default);

    Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}
