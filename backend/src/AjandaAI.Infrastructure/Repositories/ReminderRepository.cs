// IReminderRepository'nin EF Core implementasyonudur.
// Okumalar AsNoTracking; sahiplik Activity.UserId üzerinden join ile sorgu içinde uygulanır.
// Detay sorgusu Activity'yi Include eder.
// Update yalnızca skaler alanları kopyalar (navigation graph'i takibe alınmaz).

using AjandaAI.Application.Reminders;
using AjandaAI.Application.Reminders.Dtos;
using AjandaAI.Application.Common;
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

    public async Task<PagedResult<Reminder>> GetPagedAsync(ReminderFilterDto filter, int userId, CancellationToken cancellationToken = default)
    {
        var query = _context.Reminders.AsNoTracking().Where(r => r.Activity.UserId == userId);
        if (filter.ActivityId.HasValue) query = query.Where(r => r.ActivityId == filter.ActivityId.Value);
        if (filter.IsSent.HasValue) query = query.Where(r => r.IsSent == filter.IsSent.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(r => r.RemindAt)
            .ThenBy(r => r.Id)
            .Skip(filter.Skip)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);
        return new PagedResult<Reminder>(items, totalCount, filter.Page, filter.PageSize);
    }

    public Task<Reminder?> GetByIdForUserAsync(int id, int userId, CancellationToken cancellationToken = default)
    {
        return _context.Reminders
            .AsNoTracking()
            .Include(r => r.Activity)
            .FirstOrDefaultAsync(r => r.Id == id && r.Activity.UserId == userId, cancellationToken);
    }

    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        return _context.Reminders.AnyAsync(r => r.Id == id, cancellationToken);
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
