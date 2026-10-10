// Admin kullanıcı yönetimi: listeleme, ekleme, güncelleme (rol dahil), pasife alma (ADR 0018 Karar 6, 7).
// Ekleme kayıttaki doğrulama kurallarını (şifre dahil) RegisterDto validator'ı ile uygular; admin'in
// oluşturduğu kullanıcı doğrulanmış sayılır. Rol Admin → User düşürülürse ve kullanıcı pasife
// alınırsa kullanıcının tüm refresh token'ları revoke edilir; access token en geç 15 dakikada düşer.

using AjandaAI.Application.Admin.Dtos;
using AjandaAI.Application.Auth;
using AjandaAI.Application.Auth.Dtos;
using AjandaAI.Application.Common;
using AjandaAI.Application.Common.Logging;
using AjandaAI.Application.Users;
using AjandaAI.Application.Users.Dtos;
using AjandaAI.Application.Users.Validators;
using AjandaAI.Domain.Entities;
using AjandaAI.Domain.Enums;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace AjandaAI.Application.Admin;

public class AdminUserService
{
    private const string DuplicateEmailMessage = "Bu email ile kayıtlı bir kullanıcı zaten var.";
    private const string RoleMessage = "Rol zorunludur: User veya Admin olmalıdır.";

    private readonly ICurrentUser _currentUser;
    private readonly IAdminRepository _admin;
    private readonly IUserRepository _users;
    private readonly IAuthRepository _auth;
    private readonly IPasswordService _passwords;
    private readonly IValidator<RegisterDto> _createValidator;
    private readonly IValidator<UserUpdateDto> _updateValidator;
    private readonly ILogger<AdminUserService> _logger;

    public AdminUserService(
        ICurrentUser currentUser,
        IAdminRepository admin,
        IUserRepository users,
        IAuthRepository auth,
        IPasswordService passwords,
        IValidator<RegisterDto> createValidator,
        IValidator<UserUpdateDto> updateValidator,
        ILogger<AdminUserService> logger)
    {
        _currentUser = currentUser;
        _admin = admin;
        _users = users;
        _auth = auth;
        _passwords = passwords;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _logger = logger;
    }

    public async Task<ApiResponse<PagedResult<AdminUserDto>>> GetAllAsync(AdminUserFilterDto filter, CancellationToken cancellationToken = default) =>
        ApiResponse<PagedResult<AdminUserDto>>.Ok(await _admin.GetUsersPagedAsync(filter, cancellationToken));

    public async Task<ApiResponse<AdminUserDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(id, cancellationToken);
        return user is null
            ? ApiResponse<AdminUserDto>.NotFound(NotFoundMessage(id))
            : ApiResponse<AdminUserDto>.Ok(ToDto(user));
    }

    public async Task<ApiResponse<AdminUserDto>> CreateAsync(AdminUserCreateDto dto, CancellationToken cancellationToken = default)
    {
        var result = await _createValidator.ValidateAsync(
            new RegisterDto(dto.Email, dto.DisplayName, dto.TimeZoneId, dto.Password), cancellationToken);
        var errors = result.Errors.Select(e => e.ErrorMessage).ToList();
        if (!IsValidRole(dto.Role))
            errors.Add(RoleMessage);
        if (result.IsValid && await _users.EmailExistsAsync(dto.Email, null, cancellationToken))
            errors.Add(DuplicateEmailMessage);
        if (errors.Count > 0)
        {
            _logger.LogWarning("Admin kullanıcı oluşturma doğrulama hatası {AdminId} {Email} {@Errors}",
                _currentUser.UserId, MaskingHelper.MaskEmail(dto.Email), errors);
            return ApiResponse<AdminUserDto>.Fail("Doğrulama hatası.", errors);
        }

        var now = DateTimeOffset.UtcNow;
        var user = new User
        {
            Email = dto.Email.Trim(),
            DisplayName = dto.DisplayName.Trim(),
            TimeZoneId = dto.TimeZoneId.Trim(),
            PasswordHash = _passwords.Hash(dto.Password),
            Role = dto.Role!.Value,
            EmailConfirmedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };
        try
        {
            await _users.AddAsync(user, cancellationToken);
        }
        catch (DuplicateEmailException)
        {
            return ApiResponse<AdminUserDto>.Conflict(DuplicateEmailMessage);
        }
        _logger.LogInformation("Admin kullanıcı oluşturdu {AdminId} {UserId} {Email} {Role}",
            _currentUser.UserId, user.Id, MaskingHelper.MaskEmail(user.Email), user.Role);
        return ApiResponse<AdminUserDto>.Created(ToDto(user), "Kullanıcı oluşturuldu.");
    }

    public async Task<ApiResponse<AdminUserDto>> UpdateAsync(int id, AdminUserUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(id, cancellationToken);
        if (user is null)
            return ApiResponse<AdminUserDto>.NotFound(NotFoundMessage(id));

        var context = new ValidationContext<UserUpdateDto>(new UserUpdateDto(dto.Email, dto.DisplayName, dto.TimeZoneId));
        context.RootContextData[UserUpdateDtoValidator.IdKey] = id;
        var result = await _updateValidator.ValidateAsync(context, cancellationToken);
        var errors = result.Errors.Select(e => e.ErrorMessage).ToList();
        if (!IsValidRole(dto.Role))
            errors.Add(RoleMessage);
        if (errors.Count > 0)
        {
            _logger.LogWarning("Admin kullanıcı güncelleme doğrulama hatası {AdminId} {UserId} {@Errors}",
                _currentUser.UserId, id, errors);
            return ApiResponse<AdminUserDto>.Fail("Doğrulama hatası.", errors);
        }

        var newRole = dto.Role!.Value;
        var demoted = user.Role == UserRole.Admin && newRole != UserRole.Admin;
        var now = DateTimeOffset.UtcNow;
        user.Email = dto.Email.Trim();
        user.DisplayName = dto.DisplayName.Trim();
        user.TimeZoneId = dto.TimeZoneId.Trim();
        user.Role = newRole;
        user.UpdatedAt = now;
        try
        {
            await _users.UpdateAsync(user, cancellationToken);
        }
        catch (DuplicateEmailException)
        {
            return ApiResponse<AdminUserDto>.Conflict(DuplicateEmailMessage);
        }

        // AUTH-39: rol düşürmede oturumlar kapanır (ADR 0018 Karar 7).
        if (demoted)
        {
            await _auth.RevokeAllRefreshTokensAsync(id, now, cancellationToken);
            _logger.LogInformation("Kullanıcının rolü düşürüldü, refresh token'ları revoke edildi {AdminId} {UserId}",
                _currentUser.UserId, id);
        }
        _logger.LogInformation("Admin kullanıcı güncelledi {AdminId} {UserId} {Role}", _currentUser.UserId, id, user.Role);
        return ApiResponse<AdminUserDto>.Ok(ToDto(user), "Kullanıcı güncellendi.");
    }

    public async Task<ApiResponse<AdminUserDto>> DeactivateAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(id, cancellationToken);
        if (user is null)
            return ApiResponse<AdminUserDto>.NotFound(NotFoundMessage(id));

        if (user.IsActive)
        {
            var now = DateTimeOffset.UtcNow;
            user.IsActive = false;
            user.UpdatedAt = now;
            await _users.UpdateAsync(user, cancellationToken);
            await _auth.RevokeAllRefreshTokensAsync(id, now, cancellationToken);
            _logger.LogInformation("Admin kullanıcıyı pasife aldı, refresh token'ları revoke edildi {AdminId} {UserId}",
                _currentUser.UserId, id);
        }
        return ApiResponse<AdminUserDto>.Ok(ToDto(user), "Kullanıcı pasife alındı.");
    }

    // JsonStringEnumConverter sayıyı da kabul eder ("role": 7); tanımsız değer ve eksik alan reddedilir.
    private static bool IsValidRole(UserRole? role) => role is { } r && Enum.IsDefined(r);

    private static string NotFoundMessage(int id) => $"User {id} bulunamadı.";

    private static AdminUserDto ToDto(User u) =>
        new(u.Id, u.Email, u.DisplayName, u.TimeZoneId, u.Role, u.IsActive, u.EmailConfirmedAt, u.CreatedAt, u.UpdatedAt);
}
