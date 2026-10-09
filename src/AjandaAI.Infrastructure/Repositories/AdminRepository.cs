// IAdminRepository'nin EF Core implementasyonu. Sorgular sahiplik koşulsuzdur (yalnızca admin uçları).
// Aktivite/hatırlatma sorguları Select ile üstveri DTO'suna projekte edilir; içerik kolonları SQL'e girmez.

using AjandaAI.Application.Admin;
using AjandaAI.Application.Admin.Dtos;
using AjandaAI.Application.Common;
using AjandaAI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AjandaAI.Infrastructure.Repositories;

public class AdminRepository : IAdminRepository
{
    private readonly AppDbContext _context;

    public AdminRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<AdminUserDto>> GetUsersPagedAsync(AdminUserFilterDto filter, CancellationToken cancellationToken = default)
    {
        var query = _context.Users.AsNoTracking();
        if (filter.IsActive is { } isActive)
            query = query.Where(u => u.IsActive == isActive);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim().ToLower();
            query = query.Where(u => u.Email.ToLower().Contains(search) || u.DisplayName.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(u => u.CreatedAt).ThenByDescending(u => u.Id)
            .Skip(filter.Skip).Take(filter.PageSize)
            .Select(u => new AdminUserDto(u.Id, u.Email, u.DisplayName, u.TimeZoneId, u.Role, u.IsActive,
                u.EmailConfirmedAt, u.CreatedAt, u.UpdatedAt))
            .ToListAsync(cancellationToken);
        return new PagedResult<AdminUserDto>(items, totalCount, filter.Page, filter.PageSize);
    }

    public async Task<PagedResult<AdminActivityDto>> GetActivitiesPagedAsync(AdminActivityFilterDto filter, CancellationToken cancellationToken = default)
    {
        var query = _context.Activities.AsNoTracking();
        if (filter.UserId is { } userId)
            query = query.Where(a => a.UserId == userId);
        if (filter.Status is { } status)
            query = query.Where(a => a.Status == status);
        if (filter.IsActive is { } isActive)
            query = query.Where(a => a.IsActive == isActive);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(a => a.Start).ThenByDescending(a => a.Id)
            .Skip(filter.Skip).Take(filter.PageSize)
            .Select(a => new AdminActivityDto(a.Id, a.UserId, a.CategoryId, a.Status, a.Priority,
                a.Start, a.End, a.IsActive, a.CreatedAt, a.UpdatedAt))
            .ToListAsync(cancellationToken);
        return new PagedResult<AdminActivityDto>(items, totalCount, filter.Page, filter.PageSize);
    }

    public Task<AdminActivityDto?> GetActivityAsync(int id, CancellationToken cancellationToken = default) =>
        _context.Activities.AsNoTracking()
            .Where(a => a.Id == id)
            .Select(a => new AdminActivityDto(a.Id, a.UserId, a.CategoryId, a.Status, a.Priority,
                a.Start, a.End, a.IsActive, a.CreatedAt, a.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<PagedResult<AdminReminderDto>> GetRemindersPagedAsync(AdminReminderFilterDto filter, CancellationToken cancellationToken = default)
    {
        var query = _context.Reminders.AsNoTracking();
        if (filter.UserId is { } userId)
            query = query.Where(r => r.Activity.UserId == userId);
        if (filter.ActivityId is { } activityId)
            query = query.Where(r => r.ActivityId == activityId);
        if (filter.IsSent is { } isSent)
            query = query.Where(r => r.IsSent == isSent);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(r => r.RemindAt).ThenByDescending(r => r.Id)
            .Skip(filter.Skip).Take(filter.PageSize)
            .Select(r => new AdminReminderDto(r.Id, r.ActivityId, r.Activity.UserId, r.RemindAt, r.IsSent,
                r.SentAt, r.CreatedAt))
            .ToListAsync(cancellationToken);
        return new PagedResult<AdminReminderDto>(items, totalCount, filter.Page, filter.PageSize);
    }

    public async Task<bool> DeactivateActivityAsync(int id, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        if (!await _context.Activities.AnyAsync(a => a.Id == id, cancellationToken))
            return false;

        await _context.Activities
            .Where(a => a.Id == id && a.IsActive)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.IsActive, false).SetProperty(a => a.UpdatedAt, now), cancellationToken);
        return true;
    }
}
