// IAuthRepository'nin EF Core implementasyonudur (kullanıcı arama, refresh ve doğrulama token'ları).
// Çifte kullanıma açık geçişler (revoke, token kullanımı) ExecuteUpdate ile tek atomik SQL'dir.
// Postgres 23505 (unique) ihlali DuplicateEmailException'a çevrilir (ADR 0013).

using AjandaAI.Application.Auth;
using AjandaAI.Application.Common;
using AjandaAI.Application.Users;
using AjandaAI.Domain.Entities;
using AjandaAI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AjandaAI.Infrastructure.Repositories;

public class AuthRepository : IAuthRepository
{
    private readonly AppDbContext _context;

    public AuthRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalized, cancellationToken);
    }

    public async Task AddUserAsync(User user, EmailVerificationToken token, CancellationToken cancellationToken = default)
    {
        token.User = user;
        _context.Users.Add(user);
        _context.EmailVerificationTokens.Add(token);
        await SaveChangesAsync(cancellationToken);
    }

    public async Task ReplaceUnverifiedUserAsync(int oldUserId, User user, EmailVerificationToken token, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        // Yalnızca hâlâ doğrulanmamışsa silinir; token'lar DB cascade ile gider (ADR 0018 Karar 8).
        await _context.Users
            .Where(u => u.Id == oldUserId && u.EmailConfirmedAt == null)
            .ExecuteDeleteAsync(cancellationToken);

        token.User = user;
        _context.Users.Add(user);
        _context.EmailVerificationTokens.Add(token);
        await SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    public Task UpdatePasswordHashAsync(int userId, string passwordHash, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        return _context.Users
            .Where(u => u.Id == userId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(u => u.PasswordHash, passwordHash)
                .SetProperty(u => u.UpdatedAt, now), cancellationToken);
    }

    public Task<EmailVerificationToken?> GetVerificationTokenAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return _context.EmailVerificationTokens
            .AsNoTracking()
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);
    }

    public async Task<bool> ConfirmEmailAsync(int tokenId, int userId, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        var tokenRows = await _context.EmailVerificationTokens
            .Where(t => t.Id == tokenId && t.UsedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.UsedAt, (DateTimeOffset?)now), cancellationToken);
        if (tokenRows == 0)
            return false;

        var userRows = await _context.Users
            .Where(u => u.Id == userId && u.IsActive && u.EmailConfirmedAt == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(u => u.EmailConfirmedAt, (DateTimeOffset?)now)
                .SetProperty(u => u.UpdatedAt, now), cancellationToken);
        if (userRows == 0)
            return false; // transaction dispose'ta geri alınır; token kullanılmış sayılmaz.

        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task AddRefreshTokenAsync(RefreshToken token, CancellationToken cancellationToken = default)
    {
        _context.RefreshTokens.Add(token);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<RefreshToken?> GetRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return _context.RefreshTokens
            .AsNoTracking()
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);
    }

    public async Task<bool> TryRevokeRefreshTokenAsync(int tokenId, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var rows = await _context.RefreshTokens
            .Where(t => t.Id == tokenId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, (DateTimeOffset?)now), cancellationToken);
        return rows > 0;
    }

    public Task RevokeAllRefreshTokensAsync(int userId, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        return _context.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, (DateTimeOffset?)now), cancellationToken);
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
