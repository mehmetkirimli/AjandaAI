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

    // Kişisel erişim anahtarları (ADR 0024 §1).

    /// <summary>İptal edilmemiş ve süresi dolmamış anahtar sayısı.</summary>
    Task<int> CountActivePersonalAccessTokensAsync(int userId, DateTimeOffset now, CancellationToken cancellationToken = default);

    Task AddPersonalAccessTokenAsync(PersonalAccessToken token, CancellationToken cancellationToken = default);

    /// <summary>Kullanıcının tüm anahtarları (en yeni önce), izlenmez.</summary>
    Task<List<PersonalAccessToken>> GetPersonalAccessTokensAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>Id ve sahibe göre anahtar; başkasınınsa null. İzlenmez.</summary>
    Task<PersonalAccessToken?> GetPersonalAccessTokenAsync(int id, int userId, CancellationToken cancellationToken = default);

    /// <summary>Hash'e göre anahtar; User dahil, izlenmez.</summary>
    Task<PersonalAccessToken?> GetPersonalAccessTokenByHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task RevokePersonalAccessTokenAsync(int id, int userId, DateTimeOffset now, CancellationToken cancellationToken = default);

    Task TouchPersonalAccessTokenAsync(int id, DateTimeOffset now, CancellationToken cancellationToken = default);
}
