// IUserRepository'nin EF Core implementasyonudur.
// Okuma sorguları AsNoTracking ile çalışır; yazma metodları kendi SaveChanges'ını yapar.

using AjandaAI.Application.Users;
using AjandaAI.Application.Users.Dtos;
using AjandaAI.Application.Common;
using AjandaAI.Domain.Entities;
using AjandaAI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AjandaAI.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<User>> GetPagedAsync(UserFilterDto filter, CancellationToken cancellationToken = default)
    {
        var query = _context.Users.AsNoTracking().Where(u => u.IsActive);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim().ToLower();
            query = query.Where(u => u.Email.ToLower().Contains(search) || u.DisplayName.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(u => u.CreatedAt)
            .ThenByDescending(u => u.Id)
            .Skip(filter.Skip)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);
        return new PagedResult<User>(items, totalCount, filter.Page, filter.PageSize);
    }

    public Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public Task<bool> IsActiveAsync(int id, CancellationToken cancellationToken = default)
    {
        return _context.Users.AnyAsync(u => u.Id == id && u.IsActive, cancellationToken);
    }

    public Task<bool> EmailExistsAsync(string email, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToLower();
        return _context.Users.AnyAsync(
            u => u.Email.ToLower() == normalized && (excludeId == null || u.Id != excludeId),
            cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        _context.Users.Add(user);
        await SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        _context.Users.Update(user);
        await SaveChangesAsync(cancellationToken);
    }

    // users tablosundaki tek unique kısıt email index'idir; 23505 DuplicateEmailException'a çevrilir.
    private async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new DuplicateEmailException(ex);
        }
    }
}
