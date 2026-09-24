// Category lookup tablosuna erişim sözleşmesidir.
// Silme metodu yoktur: kategori pasife alınır (IsActive = false, UpdateAsync ile).
// Implementasyonu Infrastructure/Repositories/CategoryRepository.cs içindedir.

using AjandaAI.Domain.Entities;

namespace AjandaAI.Application.Categories;

public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> GetActiveAsync(CancellationToken cancellationToken = default);

    Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> IsActiveAsync(int id, CancellationToken cancellationToken = default);

    Task AddAsync(Category category, CancellationToken cancellationToken = default);

    Task<bool> NameExistsAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default);

    Task UpdateAsync(Category category, CancellationToken cancellationToken = default);
}
