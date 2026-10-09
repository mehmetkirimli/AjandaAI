// JWT access token ile rastgele (refresh / e-posta doğrulama) token üretimi ve hash'leme sözleşmesi.
// Süreler JwtOptions'tan okunur; implementasyon Infrastructure/Auth/TokenService'tedir.
// Rastgele token'ın düz metni yalnızca istemciye gider, DB'ye SHA-256 hash'i yazılır (ADR 0018 Karar 2).

using AjandaAI.Domain.Entities;

namespace AjandaAI.Application.Auth;

public sealed record AccessTokenResult(string Token, DateTimeOffset ExpiresAt);

public sealed record RandomTokenResult(string Token, string TokenHash, DateTimeOffset ExpiresAt);

public interface ITokenService
{
    AccessTokenResult CreateAccessToken(User user, DateTimeOffset now);

    /// <summary>64 byte rastgele refresh token; ömrü JwtOptions.RefreshTokenDays.</summary>
    RandomTokenResult CreateRefreshToken(DateTimeOffset now);

    /// <summary>Tek kullanımlık e-posta doğrulama token'ı; ömrü 12 saat.</summary>
    RandomTokenResult CreateVerificationToken(DateTimeOffset now);

    /// <summary>Düz metin token'ın SHA-256 hash'i (küçük harf hex, 64 karakter).</summary>
    string HashToken(string token);
}
