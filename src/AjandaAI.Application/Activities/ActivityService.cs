// Activity okuma ve yazma senaryolarını yöneten uygulama servisidir.
// Girdiyi validator ile doğrular (ilişkisel kontroller validator'dadır, burada tekrarlanmaz).
// Entity'leri DTO'ya çevirir; enum alanları string olarak döner.
// Liste yalnızca aktifleri filtreli ve sayfalı döner; Delete gerçek silme yapmaz, IsActive = false yapar.

using AjandaAI.Application.Activities.Dtos;
using AjandaAI.Application.Common;
using AjandaAI.Domain.Entities;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace AjandaAI.Application.Activities;

public class ActivityService
{
    private readonly IActivityRepository _repository;
    private readonly IValidator<ActivityCreateDto> _createValidator;
    private readonly IValidator<ActivityUpdateDto> _updateValidator;
    private readonly IValidator<ActivityFilterDto> _filterValidator;
    private readonly ILogger<ActivityService> _logger;

    public ActivityService(
        IActivityRepository repository,
        IValidator<ActivityCreateDto> createValidator,
        IValidator<ActivityUpdateDto> updateValidator,
        IValidator<ActivityFilterDto> filterValidator,
        ILogger<ActivityService> logger)
    {
        _repository = repository;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _filterValidator = filterValidator;
        _logger = logger;
    }

    public async Task<ApiResponse<PagedResult<ActivityListDto>>> GetAllAsync(ActivityFilterDto filter, CancellationToken cancellationToken = default)
    {
        var result = await _filterValidator.ValidateAsync(filter, cancellationToken);
        if (!result.IsValid)
        {
            var errors = result.Errors.Select(e => e.ErrorMessage).ToList();
            return ApiResponse<PagedResult<ActivityListDto>>.Fail("Doğrulama hatası.", errors);
        }

        var page = await _repository.GetPagedAsync(filter, cancellationToken);
        return ApiResponse<PagedResult<ActivityListDto>>.Ok(page.Map(ToListDto));
    }

    public async Task<ApiResponse<ActivityDetailDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var activity = await _repository.GetByIdAsync(id, cancellationToken);
        return activity is null
            ? ApiResponse<ActivityDetailDto>.NotFound(NotFoundMessage(id))
            : ApiResponse<ActivityDetailDto>.Ok(ToDetailDto(activity));
    }

    public async Task<ApiResponse<ActivityDetailDto>> CreateAsync(ActivityCreateDto dto, CancellationToken cancellationToken = default)
    {
        var result = await _createValidator.ValidateAsync(dto, cancellationToken);
        if (!result.IsValid)
        {
            var errors = result.Errors.Select(e => e.ErrorMessage).ToList();
            _logger.LogWarning("Aktivite oluşturma doğrulama hatası {UserId} {@Errors}", dto.UserId, errors);
            return ApiResponse<ActivityDetailDto>.Fail("Doğrulama hatası.", errors);
        }

        var now = DateTimeOffset.UtcNow;
        var activity = new Activity
        {
            UserId = dto.UserId,
            CreatedAt = now,
            UpdatedAt = now
        };
        Apply(activity, dto.CategoryId, dto.Title, dto.Description, dto.Status, dto.Priority, dto.EnergyLevel,
            dto.Start, dto.End, dto.IsAllDay, dto.Location, dto.IsFlexible, dto.EstimatedBudget, dto.Rating, dto.WouldRepeat);

        await _repository.AddAsync(activity, cancellationToken);
        _logger.LogInformation("Aktivite oluşturuldu {ActivityId} {UserId} {Title}", activity.Id, dto.UserId, activity.Title);
        return ApiResponse<ActivityDetailDto>.Created(ToDetailDto(activity), "Aktivite oluşturuldu.");
    }

    public async Task<ApiResponse<ActivityDetailDto>> UpdateAsync(int id, ActivityUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var activity = await _repository.GetByIdAsync(id, cancellationToken);
        if (activity is null)
        {
            return ApiResponse<ActivityDetailDto>.NotFound(NotFoundMessage(id));
        }

        var result = await _updateValidator.ValidateAsync(dto, cancellationToken);
        if (!result.IsValid)
        {
            var errors = result.Errors.Select(e => e.ErrorMessage).ToList();
            _logger.LogWarning("Aktivite güncelleme doğrulama hatası {ActivityId} {@Errors}", id, errors);
            return ApiResponse<ActivityDetailDto>.Fail("Doğrulama hatası.", errors);
        }

        Apply(activity, dto.CategoryId, dto.Title, dto.Description, dto.Status, dto.Priority, dto.EnergyLevel,
            dto.Start, dto.End, dto.IsAllDay, dto.Location, dto.IsFlexible, dto.EstimatedBudget, dto.Rating, dto.WouldRepeat);
        activity.UpdatedAt = DateTimeOffset.UtcNow;

        await _repository.UpdateAsync(activity, cancellationToken);
        _logger.LogInformation("Aktivite güncellendi {ActivityId} {Title}", id, activity.Title);
        return ApiResponse<ActivityDetailDto>.Ok(ToDetailDto(activity), "Aktivite güncellendi.");
    }

    public async Task<ApiResponse<bool>> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var activity = await _repository.GetByIdAsync(id, cancellationToken);
        if (activity is null)
        {
            return ApiResponse<bool>.NotFound(NotFoundMessage(id));
        }

        if (activity.IsActive)
        {
            activity.IsActive = false;
            activity.UpdatedAt = DateTimeOffset.UtcNow;
            await _repository.UpdateAsync(activity, cancellationToken);
            _logger.LogInformation("Aktivite pasife alındı {ActivityId}", id);
        }
        return ApiResponse<bool>.Ok(true, "Aktivite silindi.");
    }

    private static string NotFoundMessage(int id) => $"Activity {id} bulunamadı.";

    private static void Apply(
        Activity a, int categoryId, string title, string description,
        Domain.Enums.ActivityStatus status, Domain.Enums.Priority priority, Domain.Enums.EnergyLevel energyLevel,
        DateTimeOffset start, DateTimeOffset end, bool isAllDay, string? location, bool isFlexible,
        decimal estimatedBudget, int? rating, bool? wouldRepeat)
    {
        a.CategoryId = categoryId;
        a.Title = title;
        a.Description = description ?? string.Empty;
        a.Status = status;
        a.Priority = priority;
        a.EnergyLevel = energyLevel;
        a.Start = start;
        a.End = end;
        a.IsAllDay = isAllDay;
        a.Location = location;
        a.IsFlexible = isFlexible;
        a.EstimatedBudget = estimatedBudget;
        a.Rating = rating;
        a.WouldRepeat = wouldRepeat;
    }

    private static ActivityListDto ToListDto(Activity a) => new(
        a.Id, a.CategoryId, a.Title, a.Status.ToString(), a.Priority.ToString(),
        a.Start, a.End, a.IsAllDay);

    private static ActivityDetailDto ToDetailDto(Activity a) => new(
        a.Id, a.UserId, a.CategoryId, a.Title, a.Description,
        a.Status.ToString(), a.Priority.ToString(), a.EnergyLevel.ToString(),
        a.Start, a.End, a.IsAllDay, a.Location, a.IsFlexible, a.EstimatedBudget,
        a.Rating, a.WouldRepeat, a.IsActive, a.CreatedAt, a.UpdatedAt);
}
