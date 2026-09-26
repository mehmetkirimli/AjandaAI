// User okuma ve yazma senaryolarını yöneten uygulama servisidir.
// Doğrulama validator'larla elle yapılır; hata ApiResponse.Fail olarak döner.
// Kayıt yoksa ApiResponse.NotFound döner; HTTP status kodunu ApiResponseFilter belirler.
// Liste yalnızca aktifleri döner; Delete gerçek silme yapmaz, IsActive = false yapar.
// Validator'ı atlatan eşzamanlı email çakışması DB index'ine takılır ve Conflict (409) döner.

using AjandaAI.Application.Common;
using AjandaAI.Application.Common.Logging;
using AjandaAI.Application.Users.Dtos;
using AjandaAI.Application.Users.Validators;
using AjandaAI.Domain.Entities;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace AjandaAI.Application.Users;

public class UserService
{
    private readonly IUserRepository _repository;
    private readonly IValidator<UserCreateDto> _createValidator;
    private readonly IValidator<UserUpdateDto> _updateValidator;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUserRepository repository,
        IValidator<UserCreateDto> createValidator,
        IValidator<UserUpdateDto> updateValidator,
        ILogger<UserService> logger)
    {
        _repository = repository;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _logger = logger;
    }

    public async Task<ApiResponse<PagedResult<UserListDto>>> GetAllAsync(UserFilterDto filter, CancellationToken cancellationToken = default)
    {
        var page = await _repository.GetPagedAsync(filter, cancellationToken);
        return ApiResponse<PagedResult<UserListDto>>.Ok(page.Map(u => new UserListDto(u.Id, u.Email, u.DisplayName)));
    }

    public async Task<ApiResponse<UserDetailDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await _repository.GetByIdAsync(id, cancellationToken);
        return user is null
            ? ApiResponse<UserDetailDto>.NotFound(NotFoundMessage(id))
            : ApiResponse<UserDetailDto>.Ok(ToDetail(user));
    }

    public async Task<ApiResponse<UserDetailDto>> CreateAsync(UserCreateDto dto, CancellationToken cancellationToken = default)
    {
        var result = await _createValidator.ValidateAsync(dto, cancellationToken);
        if (!result.IsValid)
        {
            var errors = result.Errors.Select(e => e.ErrorMessage).ToList();
            _logger.LogWarning("Kullanıcı oluşturma doğrulama hatası {Email} {@Errors}",
                MaskingHelper.MaskEmail(dto.Email), errors);
            return ApiResponse<UserDetailDto>.Fail("Doğrulama hatası.", errors);
        }

        var now = DateTimeOffset.UtcNow;
        var user = new User
        {
            Email = dto.Email.Trim(),
            DisplayName = dto.DisplayName.Trim(),
            TimeZoneId = dto.TimeZoneId.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };
        try
        {
            await _repository.AddAsync(user, cancellationToken);
        }
        catch (DuplicateEmailException)
        {
            return ApiResponse<UserDetailDto>.Conflict(DuplicateEmailMessage);
        }
        _logger.LogInformation("Kullanıcı oluşturuldu {UserId} {Email}", user.Id, MaskingHelper.MaskEmail(user.Email));
        return ApiResponse<UserDetailDto>.Created(ToDetail(user), "Kullanıcı oluşturuldu.");
    }

    public async Task<ApiResponse<UserDetailDto>> UpdateAsync(int id, UserUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var user = await _repository.GetByIdAsync(id, cancellationToken);
        if (user is null)
            return ApiResponse<UserDetailDto>.NotFound(NotFoundMessage(id));

        var context = new ValidationContext<UserUpdateDto>(dto);
        context.RootContextData[UserUpdateDtoValidator.IdKey] = id;
        var result = await _updateValidator.ValidateAsync(context, cancellationToken);
        if (!result.IsValid)
        {
            var errors = result.Errors.Select(e => e.ErrorMessage).ToList();
            _logger.LogWarning("Kullanıcı güncelleme doğrulama hatası {UserId} {@Errors}", id, errors);
            return ApiResponse<UserDetailDto>.Fail("Doğrulama hatası.", errors);
        }

        user.Email = dto.Email.Trim();
        user.DisplayName = dto.DisplayName.Trim();
        user.TimeZoneId = dto.TimeZoneId.Trim();
        user.UpdatedAt = DateTimeOffset.UtcNow;
        try
        {
            await _repository.UpdateAsync(user, cancellationToken);
        }
        catch (DuplicateEmailException)
        {
            return ApiResponse<UserDetailDto>.Conflict(DuplicateEmailMessage);
        }
        _logger.LogInformation("Kullanıcı güncellendi {UserId} {Email}", id, MaskingHelper.MaskEmail(user.Email));
        return ApiResponse<UserDetailDto>.Ok(ToDetail(user), "Kullanıcı güncellendi.");
    }

    public async Task<ApiResponse<bool>> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await _repository.GetByIdAsync(id, cancellationToken);
        if (user is null)
            return ApiResponse<bool>.NotFound(NotFoundMessage(id));

        if (user.IsActive)
        {
            user.IsActive = false;
            user.UpdatedAt = DateTimeOffset.UtcNow;
            await _repository.UpdateAsync(user, cancellationToken);
            _logger.LogInformation("Kullanıcı pasife alındı {UserId}", id);
        }
        return ApiResponse<bool>.Ok(true, "Kullanıcı pasife alındı.");
    }

    private const string DuplicateEmailMessage = "Bu e-posta adresi zaten kullanılıyor.";

    private static string NotFoundMessage(int id) => $"User {id} bulunamadı.";

    private static UserDetailDto ToDetail(User u) =>
        new(u.Id, u.Email, u.DisplayName, u.TimeZoneId, u.IsActive, u.CreatedAt, u.UpdatedAt);
}
