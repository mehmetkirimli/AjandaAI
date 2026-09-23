// IActivityRepository'nin EF Core implementasyonudur.
// Salt-okunur sorgular AsNoTracking ile çalışır.

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

    public async Task<IReadOnlyList<Activity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Activities
            .AsNoTracking()
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
}
