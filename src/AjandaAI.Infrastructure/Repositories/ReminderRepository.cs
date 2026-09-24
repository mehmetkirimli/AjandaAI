// IReminderRepository'nin EF Core implementasyonudur.
// Okumalar AsNoTracking; detay sorgusu Activity'yi Include eder.
// Update yalnızca skaler alanları kopyalar (navigation graph'i takibe alınmaz).

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

    public async Task AddAsync(Reminder reminder, CancellationToken cancellationToken = default)
    {
        _context.Reminders.Add(reminder);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Reminder reminder, CancellationToken cancellationToken = default)
    {
        var tracked = await _context.Reminders.FirstAsync(r => r.Id == reminder.Id, cancellationToken);
        _context.Entry(tracked).CurrentValues.SetValues(reminder);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        return _context.Reminders.Where(r => r.Id == id).ExecuteDeleteAsync(cancellationToken);
    }
}
