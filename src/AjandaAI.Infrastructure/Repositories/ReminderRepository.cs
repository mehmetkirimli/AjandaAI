// IReminderRepository'nin EF Core implementasyonudur.
// Salt-okunur sorgular AsNoTracking ile çalışır; detay sorgusu Activity'yi Include eder.

using AjandaAI.Application.Reminders;
using AjandaAI.Domain.Entities;
using AjandaAI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AjandaAI.Infrastructure.Repositories;

public class ReminderRepository : IReminderRepository
{
    private readonly AppDbContext _context;

    public ReminderRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Reminder>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Reminders
            .AsNoTracking()
            .OrderBy(r => r.RemindAt)
            .ThenBy(r => r.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<Reminder?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return _context.Reminders
            .AsNoTracking()
            .Include(r => r.Activity)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }
}
