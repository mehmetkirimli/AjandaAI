// IActivityRepository'nin EF Core implementasyonudur.
// Salt-okunur sorgular AsNoTracking ile çalışır; yazma metodları SaveChanges çağırır.

using AjandaAI.Application.Activities;
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

    public async Task<IReadOnlyList<Activity>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Activities
            .AsNoTracking()
            .Where(a => a.IsActive)
            .OrderBy(a => a.Start)
            .ThenBy(a => a.Id)
            .ToListAsync(cancellationToken);
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
