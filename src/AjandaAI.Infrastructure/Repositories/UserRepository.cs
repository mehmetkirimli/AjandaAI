// IUserRepository'nin EF Core implementasyonudur.
// Okuma sorguları AsNoTracking ile çalışır; yazma metodları kendi SaveChanges'ını yapar.

using AjandaAI.Application.Users;
using AjandaAI.Domain.Entities;
using AjandaAI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AjandaAI.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<User>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .Where(u => u.IsActive)
            .OrderBy(u => u.Id)
            .ToListAsync(cancellationToken);
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
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
