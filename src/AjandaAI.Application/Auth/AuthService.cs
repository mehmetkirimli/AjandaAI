// Kayıt, e-posta doğrulama, giriş, token yenileme ve çıkış akışlarını yöneten servistir (ADR 0018).
// Hesap sızdırmama: register her durumda aynı 202'yi, login her hatada aynı 401 mesajını döner;
// yanıt süreleri eşitlenir (register'da kayıtlı e-postada da hash, login'de dummy hash).
// Mail gönderimi yanıtı beklemeden arka planda yapılır. Loglara şifre/token YAZILMAZ, e-posta maskelenir.

using AjandaAI.Application.Auth.Dtos;
using AjandaAI.Application.Common;
using AjandaAI.Application.Common.Logging;
using AjandaAI.Application.Users;
using AjandaAI.Domain.Entities;
using AjandaAI.Domain.Enums;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace AjandaAI.Application.Auth;

public class AuthService
{
    public const string RegisterAcceptedMessage = "Doğrulama e-postası gönderildi.";
    public const string LoginFailedMessage = "E-posta veya şifre hatalı.";
    public const string InvalidVerificationMessage = "Doğrulama bağlantısı geçersiz veya süresi dolmuş.";
    public const string InvalidSessionMessage = "Oturum geçersiz veya süresi dolmuş. Lütfen tekrar giriş yapın.";

    private readonly IAuthRepository _repository;
    private readonly IPasswordService _passwords;
    private readonly ITokenService _tokens;
    private readonly IEmailSender _emailSender;
    private readonly IValidator<RegisterDto> _registerValidator;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IAuthRepository repository,
        IPasswordService passwords,
        ITokenService tokens,
        IEmailSender emailSender,
        IValidator<RegisterDto> registerValidator,
        ILogger<AuthService> logger)
    {
        _repository = repository;
        _passwords = passwords;
        _tokens = tokens;
        _emailSender = emailSender;
        _registerValidator = registerValidator;
        _logger = logger;
    }

    public async Task<ApiResponse<object>> RegisterAsync(RegisterDto dto, CancellationToken cancellationToken = default)
    {
        var validation = await _registerValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = validation.Errors.Select(e => e.ErrorMessage).ToList();
            _logger.LogWarning("Kayıt doğrulama hatası {Email} {@Errors}", MaskingHelper.MaskEmail(dto.Email), errors);
            return ApiResponse<object>.Fail("Doğrulama hatası.", errors);
        }

        // Hash, e-posta kayıtlı olsun olmasın HER yolda hesaplanır: yanıt süresi hesap varlığını sızdırmasın.
        var passwordHash = _passwords.Hash(dto.Password);
        var email = dto.Email.Trim();
        var now = DateTimeOffset.UtcNow;

        var existing = await _repository.GetUserByEmailAsync(email, cancellationToken);
        if (existing is { EmailConfirmedAt: not null })
        {
            // Doğrulanmış hesap (aktif veya pasif): kayıt oluşmaz, sahibine bilgi maili gider.
            var existingEmail = existing.Email;
            var existingName = existing.DisplayName;
            SendInBackground("AccountExists", existingEmail,
                () => _emailSender.SendAccountAlreadyExistsAsync(existingEmail, existingName));
            _logger.LogInformation("Kayıt: doğrulanmış hesap zaten var {UserId} {Email}",
                existing.Id, MaskingHelper.MaskEmail(existingEmail));
            return ApiResponse<object>.Accepted(RegisterAcceptedMessage);
        }

        var user = new User
        {
            Email = email,
            DisplayName = dto.DisplayName.Trim(),
            TimeZoneId = dto.TimeZoneId.Trim(),
            PasswordHash = passwordHash,
            Role = UserRole.User, // Rol istemciden alınmaz (ADR 0018 Karar 5).
            EmailConfirmedAt = null,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        var verification = _tokens.CreateVerificationToken(now);
        var tokenEntity = new EmailVerificationToken
        {
            TokenHash = verification.TokenHash,
            ExpiresAt = verification.ExpiresAt,
            CreatedAt = now
        };

        try
        {
            if (existing is null)
                await _repository.AddUserAsync(user, tokenEntity, cancellationToken);
            else
                await _repository.ReplaceUnverifiedUserAsync(existing.Id, user, tokenEntity, cancellationToken);
        }
        catch (DuplicateEmailException)
        {
            // Eşzamanlı kayıt yarışı: DB index'i son savunma. Dışarıya yine aynı 202 döner.
            _logger.LogWarning("Kayıt: eşzamanlı e-posta çakışması {Email}", MaskingHelper.MaskEmail(email));
            return ApiResponse<object>.Accepted(RegisterAcceptedMessage);
        }

        var displayName = user.DisplayName;
        var plainToken = verification.Token;
        SendInBackground("EmailVerification", email,
            () => _emailSender.SendEmailVerificationAsync(email, displayName, plainToken));
        _logger.LogInformation("Kayıt alındı {UserId} {Email} {ReplacedUnverified}",
            user.Id, MaskingHelper.MaskEmail(email), existing is not null);
        return ApiResponse<object>.Accepted(RegisterAcceptedMessage);
    }

    public async Task<ApiResponse<bool>> VerifyEmailAsync(VerifyEmailDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Token))
            return ApiResponse<bool>.Fail(InvalidVerificationMessage);

        var now = DateTimeOffset.UtcNow;
        var token = await _repository.GetVerificationTokenAsync(_tokens.HashToken(dto.Token), cancellationToken);

        // Pasif kullanıcı linkle kendini açamaz (EmailConfirmedAt, IsActive'ten ayrıdır; ADR 0018 Karar 8).
        if (token is null || token.UsedAt is not null || token.ExpiresAt <= now
            || !token.User.IsActive || token.User.EmailConfirmedAt is not null)
        {
            _logger.LogWarning("E-posta doğrulama reddedildi {UserId}", token?.UserId);
            return ApiResponse<bool>.Fail(InvalidVerificationMessage);
        }

        if (!await _repository.ConfirmEmailAsync(token.Id, token.UserId, now, cancellationToken))
        {
            _logger.LogWarning("E-posta doğrulama reddedildi (yarış) {UserId}", token.UserId);
            return ApiResponse<bool>.Fail(InvalidVerificationMessage);
        }

        _logger.LogInformation("E-posta doğrulandı {UserId} {Email}",
            token.UserId, MaskingHelper.MaskEmail(token.User.Email));
        return ApiResponse<bool>.Ok(true, "E-posta adresiniz doğrulandı.");
    }

    public async Task<ApiResponse<TokenResponseDto>> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default)
    {
        var password = dto.Password ?? string.Empty; // Şifre Trim() edilmez.
        var user = string.IsNullOrWhiteSpace(dto.Email)
            ? null
            : await _repository.GetUserByEmailAsync(dto.Email.Trim(), cancellationToken);

        if (user is null)
        {
            // Hesap yoksa da aynı maliyette bir doğrulama çalışır (timing eşitleme).
            _passwords.VerifyDummy(password);
            _logger.LogWarning("Giriş başarısız: kullanıcı yok {Email}", MaskingHelper.MaskEmail(dto.Email));
            return ApiResponse<TokenResponseDto>.Unauthorized(LoginFailedMessage);
        }

        var verify = _passwords.Verify(user.PasswordHash, password);
        if (verify == PasswordVerifyResult.Failed)
        {
            _logger.LogWarning("Giriş başarısız: şifre hatalı {UserId} {Email}",
                user.Id, MaskingHelper.MaskEmail(user.Email));
            return ApiResponse<TokenResponseDto>.Unauthorized(LoginFailedMessage);
        }

        // Durum kontrolleri şifre doğrulamasından SONRA: doğrulanmamış/pasif hesap da aynı sürede ve aynı mesajla döner.
        if (user.EmailConfirmedAt is null || !user.IsActive)
        {
            _logger.LogWarning("Giriş reddedildi: hesap kullanılamaz {UserId} {Email} {EmailConfirmed} {IsActive}",
                user.Id, MaskingHelper.MaskEmail(user.Email), user.EmailConfirmedAt is not null, user.IsActive);
            return ApiResponse<TokenResponseDto>.Unauthorized(LoginFailedMessage);
        }

        var now = DateTimeOffset.UtcNow;
        if (verify == PasswordVerifyResult.SuccessRehashNeeded)
        {
            await _repository.UpdatePasswordHashAsync(user.Id, _passwords.Hash(password), now, cancellationToken);
            _logger.LogInformation("Şifre yeniden hash'lendi {UserId}", user.Id);
        }

        var response = await IssueTokensAsync(user, now, cancellationToken);
        _logger.LogInformation("Giriş başarılı {UserId} {Email}", user.Id, MaskingHelper.MaskEmail(user.Email));
        return ApiResponse<TokenResponseDto>.Ok(response, "Giriş başarılı.");
    }

    public async Task<ApiResponse<TokenResponseDto>> RefreshAsync(RefreshTokenRequestDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.RefreshToken))
            return ApiResponse<TokenResponseDto>.Unauthorized(InvalidSessionMessage);

        var now = DateTimeOffset.UtcNow;
        var stored = await _repository.GetRefreshTokenAsync(_tokens.HashToken(dto.RefreshToken), cancellationToken);
        if (stored is null)
            return ApiResponse<TokenResponseDto>.Unauthorized(InvalidSessionMessage);

        // Revoke edilmiş token tekrar kullanıldı: çalınmış olabilir, kullanıcının TÜM token'ları iptal edilir.
        if (stored.RevokedAt is not null)
            return await RejectReuseAsync(stored.UserId, now, cancellationToken);

        if (stored.ExpiresAt <= now)
            return ApiResponse<TokenResponseDto>.Unauthorized(InvalidSessionMessage);

        // Rol token'dan değil DB'den okunur (stored.User her istekte DB'den gelir; ADR 0018 Karar 2).
        var user = stored.User;
        if (!user.IsActive || user.EmailConfirmedAt is null)
        {
            await _repository.RevokeAllRefreshTokensAsync(user.Id, now, cancellationToken);
            _logger.LogWarning("Refresh reddedildi: hesap kullanılamaz {UserId}", user.Id);
            return ApiResponse<TokenResponseDto>.Unauthorized(InvalidSessionMessage);
        }

        // Rotation: eski token atomik olarak revoke edilir; eşzamanlı ikinci kullanım false alır ve yeniden kullanım sayılır.
        if (!await _repository.TryRevokeRefreshTokenAsync(stored.Id, now, cancellationToken))
            return await RejectReuseAsync(stored.UserId, now, cancellationToken);

        var response = await IssueTokensAsync(user, now, cancellationToken);
        _logger.LogInformation("Token yenilendi {UserId}", user.Id);
        return ApiResponse<TokenResponseDto>.Ok(response, "Token yenilendi.");
    }

    public async Task<ApiResponse<bool>> LogoutAsync(RefreshTokenRequestDto dto, CancellationToken cancellationToken = default)
    {
        // Idempotent: bilinmeyen/zaten revoke edilmiş token da aynı 200'ü alır (bilgi sızdırmaz).
        if (!string.IsNullOrWhiteSpace(dto.RefreshToken))
        {
            var stored = await _repository.GetRefreshTokenAsync(_tokens.HashToken(dto.RefreshToken), cancellationToken);
            if (stored is { RevokedAt: null }
                && await _repository.TryRevokeRefreshTokenAsync(stored.Id, DateTimeOffset.UtcNow, cancellationToken))
            {
                _logger.LogInformation("Çıkış yapıldı {UserId}", stored.UserId);
            }
        }
        return ApiResponse<bool>.Ok(true, "Çıkış yapıldı.");
    }

    private async Task<ApiResponse<TokenResponseDto>> RejectReuseAsync(int userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await _repository.RevokeAllRefreshTokensAsync(userId, now, cancellationToken);
        _logger.LogWarning("Revoke edilmiş refresh token yeniden kullanıldı; tüm token'lar iptal edildi {UserId}", userId);
        return ApiResponse<TokenResponseDto>.Unauthorized(InvalidSessionMessage);
    }

    private async Task<TokenResponseDto> IssueTokensAsync(User user, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var access = _tokens.CreateAccessToken(user, now);
        var refresh = _tokens.CreateRefreshToken(now);
        await _repository.AddRefreshTokenAsync(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refresh.TokenHash,
            ExpiresAt = refresh.ExpiresAt,
            CreatedAt = now
        }, cancellationToken);
        return new TokenResponseDto(access.Token, access.ExpiresAt, refresh.Token, refresh.ExpiresAt);
    }

    // Yanıt gönderimi beklemez (timing eşitleme). Hata yalnızca loglanır; scoped servis yakalanmaz, yalnızca
    // singleton IEmailSender kullanılır. Bilinen kısıt: süreç çökerse bekleyen mail kaybolur (kalıcı kuyruk v2 bildirim adımında).
    private void SendInBackground(string kind, string email, Func<Task> send)
    {
        var logger = _logger;
        var maskedEmail = MaskingHelper.MaskEmail(email);
        _ = Task.Run(async () =>
        {
            try
            {
                await send();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "E-posta gönderilemedi {Kind} {Email}", kind, maskedEmail);
            }
        });
    }
}
