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
        var query = _context.Categories.AsNoTracking().Where(c => c.IsActive);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(c => c.Id)
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
}
