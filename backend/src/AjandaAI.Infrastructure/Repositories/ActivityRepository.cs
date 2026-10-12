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

    public async Task<PagedResult<Activity>> GetPagedAsync(ActivityFilterDto filter, int userId, CancellationToken cancellationToken = default)
    {
        var query = _context.Activities.AsNoTracking().Where(a => a.UserId == userId && a.IsActive);
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

    public Task<Activity?> GetByIdForUserAsync(int id, int userId, CancellationToken cancellationToken = default)
    {
        return _context.Activities
            .AsNoTracking()
            .Include(a => a.Series)
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId, cancellationToken);
    }

    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        return _context.Activities.AnyAsync(a => a.Id == id, cancellationToken);
    }

    public async Task AddAsync(Activity activity, CancellationToken cancellationToken = default)
    {
        _context.Activities.Add(activity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Activity activity, CancellationToken cancellationToken = default)
    {
        _context.Activities.Update(activity);
        // Detay için Include edilen seri güncellenmez; Update grafiği onu Modified işaretlemesin.
        if (activity.Series is not null)
        {
            _context.Entry(activity.Series).State = EntityState.Unchanged;
        }
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddSeriesAsync(ActivitySeries series, IReadOnlyList<Activity> activities, CancellationToken cancellationToken = default)
    {
        _context.ActivitySeries.Add(series);
        foreach (var activity in activities)
        {
            activity.Series = series;
        }
        _context.Activities.AddRange(activities);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<ActivitySeries?> GetSeriesForUserAsync(int seriesId, int userId, CancellationToken cancellationToken = default)
    {
        return _context.ActivitySeries
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == seriesId && s.UserId == userId, cancellationToken);
    }

    public Task<bool> SeriesExistsAsync(int seriesId, CancellationToken cancellationToken = default)
    {
        return _context.ActivitySeries.AnyAsync(s => s.Id == seriesId, cancellationToken);
    }

    public Task<int> DeactivateSeriesFromAsync(int seriesId, int userId, DateTimeOffset fromUtc, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        return _context.Activities
            .Where(a => a.SeriesId == seriesId && a.UserId == userId && a.IsActive && a.Start >= fromUtc)
            .ExecuteUpdateAsync(set => set
                .SetProperty(a => a.IsActive, false)
                .SetProperty(a => a.UpdatedAt, now), cancellationToken);
    }
}
