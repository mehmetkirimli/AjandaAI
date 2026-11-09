// Kişisel erişim anahtarı (PAT) yönetimi ve doğrulaması (ADR 0024 §1).
// - Anahtar: "ajd_" + 32 bayt kriptografik rastgele (base64url). DB'ye yalnızca SHA-256 hex özeti ve ilk 8 karakter (Prefix).
// - Ömür 90 gün; kullanıcı başına en fazla 5 ETKİN (iptal edilmemiş, süresi dolmamış) anahtar.
// - Create/List/Revoke oturumdaki kullanıcıya (ICurrentUser, ADR 0019) bağlıdır; başkasının anahtarı 404'tür.
// - ValidateAsync HTTP'den bağımsızdır (ICurrentUser okumaz); adım 3'te /mcp authentication handler'ı çağırır.
//   Şifre değişikliği anahtarları geçersiz kılmaz (MCP-18, bilinçli).
// Anahtar ve özeti ASLA loglanmaz; loglarda yalnızca kullanıcı ve anahtar Id'si geçer.

using System.Buffers;
using System.Security.Cryptography;
using AjandaAI.Application.Auth.Dtos;
using AjandaAI.Application.Common;
using AjandaAI.Domain.Entities;
using AjandaAI.Domain.Enums;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace AjandaAI.Application.Auth;

/// <summary>Geçerli bir PAT'in sahibi.</summary>
public sealed record PersonalAccessTokenIdentity(int UserId, UserRole Role, int TokenId);

public class PersonalAccessTokenService
{
    public const string TokenPrefix = "ajd_";
    public const int MaxActiveTokens = 5;
    public const int LifetimeDays = 90;
    public const int PrefixLength = 8;
    public const string LimitMessage = "En fazla 5 etkin erişim anahtarın olabilir. Yenisini oluşturmak için birini iptal et.";
    public const string NotFoundMessage = "Erişim anahtarı bulunamadı.";

    private const int RandomBytes = 32;
    // "ajd_" + 32 baytın dolgusuz base64url hali (43 karakter).
    private const int TokenLength = 4 + 43;

    private static readonly SearchValues<char> Base64UrlChars =
        SearchValues.Create("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_");

    private readonly ICurrentUser _currentUser;
    private readonly IAuthRepository _repository;
    private readonly ITokenService _tokens;
    private readonly IValidator<PersonalAccessTokenCreateDto> _validator;
    private readonly ILogger<PersonalAccessTokenService> _logger;

    public PersonalAccessTokenService(
        ICurrentUser currentUser,
        IAuthRepository repository,
        ITokenService tokens,
        IValidator<PersonalAccessTokenCreateDto> validator,
        ILogger<PersonalAccessTokenService> logger)
    {
        _currentUser = currentUser;
        _repository = repository;
        _tokens = tokens;
        _validator = validator;
        _logger = logger;
    }

    public async Task<ApiResponse<PersonalAccessTokenCreatedDto>> CreateAsync(
        PersonalAccessTokenCreateDto dto, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId;
        var result = await _validator.ValidateAsync(dto, cancellationToken);
        if (!result.IsValid)
        {
            var errors = result.Errors.Select(e => e.ErrorMessage).ToList();
            _logger.LogWarning("Erişim anahtarı doğrulama hatası {UserId} {@Errors}", userId, errors);
            return ApiResponse<PersonalAccessTokenCreatedDto>.Fail("Doğrulama hatası.", errors);
        }

        var now = DateTimeOffset.UtcNow;
        if (await _repository.CountActivePersonalAccessTokensAsync(userId, now, cancellationToken) >= MaxActiveTokens)
        {
            _logger.LogWarning("Erişim anahtarı sınırı aşıldı {UserId}", userId);
            return ApiResponse<PersonalAccessTokenCreatedDto>.Fail(LimitMessage, [LimitMessage]);
        }

        var raw = TokenPrefix + Base64Url(RandomNumberGenerator.GetBytes(RandomBytes));
        var token = new PersonalAccessToken
        {
            UserId = userId,
            Name = dto.Name.Trim(),
            TokenHash = _tokens.HashToken(raw),
            Prefix = raw[..PrefixLength],
            CreatedAt = now,
            ExpiresAt = now.AddDays(LifetimeDays)
        };
        await _repository.AddPersonalAccessTokenAsync(token, cancellationToken);

        _logger.LogInformation("Erişim anahtarı oluşturuldu {UserId} {TokenId}", userId, token.Id);
        return ApiResponse<PersonalAccessTokenCreatedDto>.Created(
            new(token.Id, token.Name, token.Prefix, raw, token.CreatedAt, token.ExpiresAt),
            "Erişim anahtarı oluşturuldu. Bu anahtarı şimdi kopyala; bir daha gösterilmeyecek.");
    }

    public async Task<ApiResponse<List<PersonalAccessTokenDto>>> ListAsync(CancellationToken cancellationToken = default)
    {
        var tokens = await _repository.GetPersonalAccessTokensAsync(_currentUser.UserId, cancellationToken);
        return ApiResponse<List<PersonalAccessTokenDto>>.Ok(tokens.Select(ToDto).ToList());
    }

    public async Task<ApiResponse<bool>> RevokeAsync(int id, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId;
        var token = await _repository.GetPersonalAccessTokenAsync(id, userId, cancellationToken);
        if (token is null)
            return ApiResponse<bool>.NotFound(NotFoundMessage);

        if (token.RevokedAt is null)
            await _repository.RevokePersonalAccessTokenAsync(id, userId, DateTimeOffset.UtcNow, cancellationToken);

        _logger.LogInformation("Erişim anahtarı iptal edildi {UserId} {TokenId}", userId, id);
        return ApiResponse<bool>.Ok(true, "Erişim anahtarı iptal edildi.");
    }

    /// <summary>
    /// Düz anahtarı doğrular. Biçim bozuk, bilinmeyen, iptal edilmiş, süresi dolmuş veya sahibi pasifse null.
    /// Başarılıysa LastUsedAt güncellenir. HTTP'ye ve ICurrentUser'a bağımlı değildir.
    /// </summary>
    public async Task<PersonalAccessTokenIdentity?> ValidateAsync(string? rawToken, CancellationToken cancellationToken = default)
    {
        if (!IsWellFormed(rawToken))
            return null;

        var token = await _repository.GetPersonalAccessTokenByHashAsync(_tokens.HashToken(rawToken!), cancellationToken);
        if (token is null)
            return null;

        var now = DateTimeOffset.UtcNow;
        if (token.RevokedAt is not null || token.ExpiresAt <= now || !token.User.IsActive)
        {
            _logger.LogWarning("Erişim anahtarı reddedildi {UserId} {TokenId}", token.UserId, token.Id);
            return null;
        }

        await _repository.TouchPersonalAccessTokenAsync(token.Id, now, cancellationToken);
        return new PersonalAccessTokenIdentity(token.UserId, token.User.Role, token.Id);
    }

    private static bool IsWellFormed(string? raw) =>
        raw is { Length: TokenLength }
        && raw.StartsWith(TokenPrefix, StringComparison.Ordinal)
        && raw.AsSpan(TokenPrefix.Length).IndexOfAnyExcept(Base64UrlChars) < 0;

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static PersonalAccessTokenDto ToDto(PersonalAccessToken t) =>
        new(t.Id, t.Name, t.Prefix, t.CreatedAt, t.ExpiresAt, t.LastUsedAt, t.RevokedAt);
}
