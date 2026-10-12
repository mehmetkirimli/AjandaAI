// ICategoryRepository'nin EF Core implementasyonudur.
// Okuma sorguları AsNoTracking ile çalışır; yazma metodları kendi SaveChanges'ını yapar.
// Delete yoktur: kategori UpdateAsync ile pasife alınır.

using AjandaAI.Application.Categories;
using AjandaAI.Application.Categories.Dtos;
using AjandaAI.Application.Common;
using AjandaAI.Domain.Entities;
using AjandaAI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AjandaAI.Infrastructure.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly AppDbContext _context;

    public CategoryRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<Category>> GetPagedAsync(CategoryFilterDto filter, CancellationToken cancellationToken = default)
    {
        var query = _context.Categories.AsNoTracking();
        if (!filter.IncludeInactive)
            query = query.Where(c => c.IsActive);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Id)
            .Skip(filter.Skip)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);
        return new PagedResult<Category>(items, totalCount, filter.Page, filter.PageSize);
    }

    public Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return _context.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public Task<bool> IsActiveAsync(int id, CancellationToken cancellationToken = default)
    {
        return _context.Categories.AnyAsync(c => c.Id == id && c.IsActive, cancellationToken);
    }

    public async Task AddAsync(Category category, CancellationToken cancellationToken = default)
    {
        _context.Categories.Add(category);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> NameExistsAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var normalized = name.Trim().ToLower();
        return _context.Categories.AnyAsync(
            c => c.Name.ToLower() == normalized && (excludeId == null || c.Id != excludeId),
            cancellationToken);
    }

    public async Task UpdateAsync(Category category, CancellationToken cancellationToken = default)
    {
        _context.Categories.Update(category);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<List<int>> GetAllIdsAsync(CancellationToken cancellationToken = default) =>
        _context.Categories.AsNoTracking().Select(c => c.Id).ToListAsync(cancellationToken);

    public async Task<int> GetMaxSortOrderAsync(CancellationToken cancellationToken = default) =>
        await _context.Categories.MaxAsync(c => (int?)c.SortOrder, cancellationToken) ?? 0;

    public async Task ReorderAsync(IReadOnlyList<int> orderedIds, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        for (var i = 0; i < orderedIds.Count; i++)
        {
            var id = orderedIds[i];
            var order = i + 1;
            await _context.Categories
                .Where(c => c.Id == id)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.SortOrder, order), cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
    }
}
