// IActivityRepository'nin EF Core implementasyonudur.
// Salt-okunur sorgular AsNoTracking ile çalışır; yazma metodları SaveChanges çağırır.

using AjandaAI.Application.Activities;
using AjandaAI.Application.Activities.Dtos;
using AjandaAI.Application.Common;
using AjandaAI.Domain.Entities;
using AjandaAI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AjandaAI.Infrastructure.Repositories;

public class ActivityRepository : IActivityRepository
{
    private readonly AppDbContext _context;

    public ActivityRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<Activity>> GetPagedAsync(ActivityFilterDto filter, CancellationToken cancellationToken = default)
    {
        var query = _context.Activities.AsNoTracking().Where(a => a.IsActive);
        if (filter.From.HasValue) query = query.Where(a => a.Start >= filter.From.Value);
        if (filter.To.HasValue) query = query.Where(a => a.Start <= filter.To.Value);
        if (filter.Status.HasValue) query = query.Where(a => a.Status == filter.Status.Value);
        if (filter.CategoryId.HasValue) query = query.Where(a => a.CategoryId == filter.CategoryId.Value);
        if (filter.Priority.HasValue) query = query.Where(a => a.Priority == filter.Priority.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(a => a.Start)
            .ThenBy(a => a.Id)
            .Skip(filter.Skip)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);
        return new PagedResult<Activity>(items, totalCount, filter.Page, filter.PageSize);
    }

    public Task<Activity?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return _context.Activities
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public Task<bool> IsActiveAsync(int id, CancellationToken cancellationToken = default)
    {
        return _context.Activities.AnyAsync(a => a.Id == id && a.IsActive, cancellationToken);
    }

    public async Task AddAsync(Activity activity, CancellationToken cancellationToken = default)
    {
        _context.Activities.Add(activity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Activity activity, CancellationToken cancellationToken = default)
    {
        _context.Activities.Update(activity);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
