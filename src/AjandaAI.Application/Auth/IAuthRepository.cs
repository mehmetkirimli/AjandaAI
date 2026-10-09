// Auth akışlarının veri erişim sözleşmesi: kullanıcı arama, refresh token ve doğrulama token'ı.
// Atomik güncellemeler (TryRevoke, ConfirmEmail) yarış durumlarını (çifte kullanım) veritabanında çözer.
// Implementasyonu Infrastructure/Repositories/AuthRepository.cs içindedir.

using AjandaAI.Domain.Entities;

namespace AjandaAI.Application.Auth;

public interface IAuthRepository
{
    /// <summary>E-postaya göre kullanıcı (büyük/küçük harf duyarsız, aktif/pasif fark etmez). İzlenmez.</summary>
    Task<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>Yeni kullanıcıyı ve doğrulama token'ını tek SaveChanges'ta ekler. Email çakışmasında DuplicateEmailException.</summary>
    Task AddUserAsync(User user, EmailVerificationToken token, CancellationToken cancellationToken = default);

    /// <summary>Doğrulanmamış eski kaydı HARD siler (token'ları cascade) ve yenisini tek transaction'da ekler.</summary>
    Task ReplaceUnverifiedUserAsync(int oldUserId, User user, EmailVerificationToken token, CancellationToken cancellationToken = default);

    Task UpdatePasswordHashAsync(int userId, string passwordHash, DateTimeOffset now, CancellationToken cancellationToken = default);

    /// <summary>Hash'e göre doğrulama token'ı; User dahil, izlenmez.</summary>
    Task<EmailVerificationToken?> GetVerificationTokenAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>Token'ı kullanıldı işaretler ve kullanıcıyı doğrular (tek transaction). Token zaten kullanılmışsa veya kullanıcı pasifse false.</summary>
    Task<bool> ConfirmEmailAsync(int tokenId, int userId, DateTimeOffset now, CancellationToken cancellationToken = default);

    Task AddRefreshTokenAsync(RefreshToken token, CancellationToken cancellationToken = default);

    /// <summary>Hash'e göre refresh token; User dahil, izlenmez.</summary>
    Task<RefreshToken?> GetRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>Yalnızca hâlâ aktifse revoke eder (atomik); bu çağrı revoke ettiyse true.</summary>
    Task<bool> TryRevokeRefreshTokenAsync(int tokenId, DateTimeOffset now, CancellationToken cancellationToken = default);

    /// <summary>Kullanıcının revoke edilmemiş tüm refresh token'larını revoke eder.</summary>
    Task RevokeAllRefreshTokensAsync(int userId, DateTimeOffset now, CancellationToken cancellationToken = default);
}
