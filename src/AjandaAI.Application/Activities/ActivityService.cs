// Activity okuma ve yazma senaryolarını yöneten uygulama servisidir.
// Girdiyi validator ile doğrular (ilişkisel kontroller validator'dadır, burada tekrarlanmaz).
// Entity'leri DTO'ya çevirir; enum alanları string olarak döner.
// Liste yalnızca aktifleri döner; Delete gerçek silme yapmaz, IsActive = false yapar.

using AjandaAI.Application.Activities.Dtos;
using AjandaAI.Application.Common;
using AjandaAI.Domain.Entities;
using FluentValidation;

namespace AjandaAI.Application.Activities;

public class ActivityService
{
    private readonly IActivityRepository _repository;
    private readonly IValidator<ActivityCreateDto> _createValidator;
    private readonly IValidator<ActivityUpdateDto> _updateValidator;

    public ActivityService(
        IActivityRepository repository,
        IValidator<ActivityCreateDto> createValidator,
        IValidator<ActivityUpdateDto> updateValidator)
    {
        _repository = repository;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<IReadOnlyList<ActivityListDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var activities = await _repository.GetActiveAsync(cancellationToken);
        return activities.Select(ToListDto).ToList();
    }

    public async Task<ActivityDetailDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var activity = await _repository.GetByIdAsync(id, cancellationToken);
        return activity is null ? null : ToDetailDto(activity);
    }

    public async Task<ApiResponse<ActivityDetailDto>> CreateAsync(ActivityCreateDto dto, CancellationToken cancellationToken = default)
    {
        var result = await _createValidator.ValidateAsync(dto, cancellationToken);
        if (!result.IsValid)
        {
            return ApiResponse<ActivityDetailDto>.Fail("Doğrulama hatası.",
                result.Errors.Select(e => e.ErrorMessage).ToList());
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
        return ApiResponse<ActivityDetailDto>.Ok(ToDetailDto(activity), "Aktivite oluşturuldu.");
    }

    /// <summary>Kayıt yoksa null döner (controller 404'e çevirir).</summary>
    public async Task<ApiResponse<ActivityDetailDto>?> UpdateAsync(int id, ActivityUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var activity = await _repository.GetByIdAsync(id, cancellationToken);
        if (activity is null)
        {
            return null;
        }

        var result = await _updateValidator.ValidateAsync(dto, cancellationToken);
        if (!result.IsValid)
        {
            return ApiResponse<ActivityDetailDto>.Fail("Doğrulama hatası.",
                result.Errors.Select(e => e.ErrorMessage).ToList());
        }

        Apply(activity, dto.CategoryId, dto.Title, dto.Description, dto.Status, dto.Priority, dto.EnergyLevel,
            dto.Start, dto.End, dto.IsAllDay, dto.Location, dto.IsFlexible, dto.EstimatedBudget, dto.Rating, dto.WouldRepeat);
        activity.UpdatedAt = DateTimeOffset.UtcNow;

        await _repository.UpdateAsync(activity, cancellationToken);
        return ApiResponse<ActivityDetailDto>.Ok(ToDetailDto(activity), "Aktivite güncellendi.");
    }

    /// <summary>Kayıt yoksa false döner.</summary>
    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var activity = await _repository.GetByIdAsync(id, cancellationToken);
        if (activity is null)
        {
            return false;
        }

        if (activity.IsActive)
        {
            activity.IsActive = false;
            activity.UpdatedAt = DateTimeOffset.UtcNow;
            await _repository.UpdateAsync(activity, cancellationToken);
        }
        return true;
    }

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
