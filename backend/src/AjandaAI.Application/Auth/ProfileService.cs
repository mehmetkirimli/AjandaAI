// Oturumdaki kullanıcının kendi profili: görüntüleme (GET /api/auth/me), düzenleme (PUT /api/auth/me)
// ve şifre değiştirme (POST /api/auth/change-password). ADR 0021, profil sayfası.
// AuthService'ten ayrıdır: AuthService IEmailSender'a bağlıdır ve o Production'da henüz kayıtlı değildir.
//
// Şifre değiştirme:
// - Mevcut şifre zorunludur; yanlışsa 400 döner (401 DEĞİL: istemci 401'de oturumu yeniler/kapatır).
// - Yeni şifre kayıttaki kurallara tabidir (RegisterDtoValidator: uzunluk, yaygın liste, kişisel bilgi).
// - Başarıda kullanıcının TÜM refresh token'ları revoke edilir, BU CİHAZ DAHİL; kullanıcı yeni şifresiyle
//   tekrar giriş yapar. Bu cihaza yeni token verilmez: diğer cihazlar revoke edilmiş token'larıyla refresh
//   denediğinde yeniden kullanım tespiti (ADR 0018) tüm oturumları zaten kapatırdı; vaat edilemeyecek bir
//   "bu cihaz açık kalır" davranışı yerine tutarlı bir "her yerden çıkış" seçildi (logout kararıyla aynı).
//   Eski access token'lar en fazla 15 dk daha geçerlidir.
// Loglara şifre yazılmaz.

using AjandaAI.Application.Auth.Dtos;
using AjandaAI.Application.Common;
using AjandaAI.Application.Users;
using AjandaAI.Domain.Entities;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace AjandaAI.Application.Auth;

public class ProfileService
{
    public const string WrongCurrentPasswordMessage = "Mevcut şifre hatalı.";

    private readonly ICurrentUser _currentUser;
    private readonly IUserRepository _users;
    private readonly IAuthRepository _auth;
    private readonly IPasswordService _passwords;
        private readonly IValidator<ProfileUpdateDto> _updateValidator;
    private readonly IValidator<RegisterDto> _passwordRules;
    private readonly ILogger<ProfileService> _logger;

    public ProfileService(
        ICurrentUser currentUser,
        IUserRepository users,
        IAuthRepository auth,
        IPasswordService passwords,
        IValidator<ProfileUpdateDto> updateValidator,
        IValidator<RegisterDto> passwordRules,
        ILogger<ProfileService> logger)
    {
        _currentUser = currentUser;
        _users = users;
        _auth = auth;
        _passwords = passwords;
        _updateValidator = updateValidator;
        _passwordRules = passwordRules;
        _logger = logger;
    }

    public async Task<ApiResponse<MeDto>> GetAsync(CancellationToken cancellationToken = default)
    {
        var user = await LoadAsync(cancellationToken);
        return user is null
            ? ApiResponse<MeDto>.Unauthorized(AuthService.InvalidSessionMessage)
            : ApiResponse<MeDto>.Ok(ToDto(user));
    }

    public async Task<ApiResponse<MeDto>> UpdateAsync(ProfileUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var user = await LoadAsync(cancellationToken);
        if (user is null)
            return ApiResponse<MeDto>.Unauthorized(AuthService.InvalidSessionMessage);

        var result = await _updateValidator.ValidateAsync(dto, cancellationToken);
        if (!result.IsValid)
        {
            var errors = result.Errors.Select(e => e.ErrorMessage).ToList();
            _logger.LogWarning("Profil güncelleme doğrulama hatası {UserId} {@Errors}", user.Id, errors);
            return ApiResponse<MeDto>.Fail("Doğrulama hatası.", errors);
        }

        user.DisplayName = dto.DisplayName.Trim();
        user.TimeZoneId = dto.TimeZoneId.Trim();
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await _users.UpdateAsync(user, cancellationToken);
        _logger.LogInformation("Profil güncellendi {UserId}", user.Id);
        return ApiResponse<MeDto>.Ok(ToDto(user), "Profil güncellendi.");
    }

    public async Task<ApiResponse<bool>> ChangePasswordAsync(ChangePasswordDto dto, CancellationToken cancellationToken = default)
    {
        var user = await LoadAsync(cancellationToken);
        if (user is null)
            return ApiResponse<bool>.Unauthorized(AuthService.InvalidSessionMessage);

        if (_passwords.Verify(user.PasswordHash, dto.CurrentPassword ?? string.Empty) == PasswordVerifyResult.Failed)
        {
            _logger.LogWarning("Şifre değiştirme reddedildi: mevcut şifre hatalı {UserId}", user.Id);
            return ApiResponse<bool>.Fail("Doğrulama hatası.", [WrongCurrentPasswordMessage]);
        }

        // Yeni şifre, kullanıcının kendi e-posta ve adıyla kayıt kurallarından geçer; yalnızca şifre hataları alınır.
        var newPassword = dto.NewPassword ?? string.Empty;
        var rules = await _passwordRules.ValidateAsync(
            new RegisterDto(user.Email, user.DisplayName, user.TimeZoneId, newPassword), cancellationToken);
        var errors = rules.Errors
            .Where(e => e.PropertyName == nameof(RegisterDto.Password))
            .Select(e => e.ErrorMessage)
            .ToList();
        if (newPassword == dto.CurrentPassword)
            errors.Add("Yeni şifre mevcut şifreden farklı olmalıdır.");
        if (errors.Count > 0)
        {
            _logger.LogWarning("Şifre değiştirme doğrulama hatası {UserId} {@Errors}", user.Id, errors);
            return ApiResponse<bool>.Fail("Doğrulama hatası.", errors);
        }

        var now = DateTimeOffset.UtcNow;
        await _auth.UpdatePasswordHashAsync(user.Id, _passwords.Hash(newPassword), now, cancellationToken);
        await _auth.RevokeAllRefreshTokensAsync(user.Id, now, cancellationToken);

        _logger.LogInformation("Şifre değiştirildi, tüm oturumlar kapatıldı {UserId}", user.Id);
        return ApiResponse<bool>.Ok(true, "Şifren değiştirildi. Güvenliğin için tüm oturumların kapatıldı; yeni şifrenle tekrar giriş yap.");
    }

    // Pasif kullanıcının token'ı en fazla 15 dk yaşar (ADR 0018); bu sürede profil işlemleri de reddedilir.
    private async Task<User?> LoadAsync(CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(_currentUser.UserId, cancellationToken);
        return user is { IsActive: true } ? user : null;
    }

    private static MeDto ToDto(User u) => new(u.Id, u.Email, u.DisplayName, u.TimeZoneId, u.Role, u.CreatedAt);
}
