// Category lookup tablosuna erişim sözleşmesidir.
// Silme metodu yoktur: kategori pasife alınır (IsActive = false, UpdateAsync ile).
// Implementasyonu Infrastructure/Repositories/CategoryRepository.cs içindedir.

using AjandaAI.Application.Categories.Dtos;
using AjandaAI.Application.Common;
using AjandaAI.Domain.Entities;

namespace AjandaAI.Application.Categories;

public interface ICategoryRepository
{
    /// <summary>Aktif kayıtları (IncludeInactive ise tümünü) SortOrder, Id sırasıyla sayfalar (önce count, sonra Skip/Take).</summary>
    Task<PagedResult<Category>> GetPagedAsync(CategoryFilterDto filter, CancellationToken cancellationToken = default);

    Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> IsActiveAsync(int id, CancellationToken cancellationToken = default);

    Task AddAsync(Category category, CancellationToken cancellationToken = default);

    Task<bool> NameExistsAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default);

    Task UpdateAsync(Category category, CancellationToken cancellationToken = default);

    /// <summary>Aktif + pasif tüm kategori Id'leri (sıralama doğrulaması için).</summary>
    Task<List<int>> GetAllIdsAsync(CancellationToken cancellationToken = default);

    /// <summary>En büyük SortOrder; kayıt yoksa 0.</summary>
    Task<int> GetMaxSortOrderAsync(CancellationToken cancellationToken = default);

    /// <summary>Verilen sırayla SortOrder = 1..N atar; tek transaction (ADR 0023 Karar 2).</summary>
    Task ReorderAsync(IReadOnlyList<int> orderedIds, CancellationToken cancellationToken = default);
}
