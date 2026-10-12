// Activity okuma ve yazma senaryolarını yöneten uygulama servisidir.
// Girdiyi validator ile doğrular (ilişkisel kontroller validator'dadır, burada tekrarlanmaz).
// Entity'leri DTO'ya çevirir; enum alanları string olarak döner.
// Sahip her zaman ICurrentUser'dır; sahiplik koşulu repository sorgusundadır (ADR 0018), serviste ayrı if yoktur.
// Başkasının kaydı "yok" ile aynı 404'tür; yalnızca bu hata yolunda ExistsAsync ile yetkisiz deneme loglanır.
// Liste yalnızca kullanıcının aktiflerini filtreli ve sayfalı döner; Delete gerçek silme yapmaz, IsActive = false yapar.

using AjandaAI.Application.Activities.Dtos;
using AjandaAI.Application.Common;
using AjandaAI.Application.Users;
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
    private readonly IValidator<ActivitySeriesCreateDto> _seriesValidator;
    private readonly IUserRepository _users;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<ActivityService> _logger;

    public ActivityService(
        IActivityRepository repository,
        IValidator<ActivityCreateDto> createValidator,
        IValidator<ActivityUpdateDto> updateValidator,
        IValidator<ActivityFilterDto> filterValidator,
        IValidator<ActivitySeriesCreateDto> seriesValidator,
        IUserRepository users,
        ICurrentUser currentUser,
        ILogger<ActivityService> logger)
    {
        _repository = repository;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _filterValidator = filterValidator;
        _seriesValidator = seriesValidator;
        _users = users;
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

        var page = await _repository.GetPagedAsync(filter, _currentUser.UserId, cancellationToken);
        return ApiResponse<PagedResult<ActivityListDto>>.Ok(page.Map(ToListDto));
    }

    public async Task<ApiResponse<ActivityDetailDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var activity = await FindOwnedAsync(id, cancellationToken);
        return activity is null
            ? ApiResponse<ActivityDetailDto>.NotFound(NotFoundMessage(id))
            : ApiResponse<ActivityDetailDto>.Ok(ToDetailDto(activity));
    }

    public async Task<ApiResponse<ActivityDetailDto>> CreateAsync(ActivityCreateDto dto, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId;
        var result = await _createValidator.ValidateAsync(dto, cancellationToken);
        if (!result.IsValid)
        {
            var errors = result.Errors.Select(e => e.ErrorMessage).ToList();
            _logger.LogWarning("Aktivite oluşturma doğrulama hatası {UserId} {@Errors}", userId, errors);
            return ApiResponse<ActivityDetailDto>.Fail("Doğrulama hatası.", errors);
        }

        var now = DateTimeOffset.UtcNow;
        var activity = new Activity
        {
            UserId = userId,
            CreatedAt = now,
            UpdatedAt = now
        };
        Apply(activity, dto.CategoryId, dto.Title, dto.Description, dto.Status, dto.Priority, dto.EnergyLevel,
            dto.Start, dto.End, dto.IsAllDay, dto.Location, dto.IsFlexible, dto.EstimatedBudget, dto.Rating, dto.WouldRepeat);

        await _repository.AddAsync(activity, cancellationToken);
        _logger.LogInformation("Aktivite oluşturuldu {ActivityId} {UserId} {Title}", activity.Id, userId, activity.Title);
        return ApiResponse<ActivityDetailDto>.Created(ToDetailDto(activity), "Aktivite oluşturuldu.");
    }

    public async Task<ApiResponse<ActivitySeriesCreatedDto>> CreateSeriesAsync(ActivitySeriesCreateDto dto, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId;
        var result = await _seriesValidator.ValidateAsync(dto, cancellationToken);
        if (!result.IsValid)
        {
            return SeriesFail(userId, result.Errors.Select(e => e.ErrorMessage).ToList());
        }

        var zone = await GetUserZoneAsync(userId, cancellationToken);
        var recurrence = dto.Recurrence!;
        var mask = recurrence.Frequency == Domain.Enums.RecurrenceFrequency.Weekly
            ? RecurrenceExpander.ToMask(recurrence.DaysOfWeek!)
            : 0;

        var firstDate = RecurrenceExpander.LocalDateOf(dto.Start, zone);
        if (recurrence.Until < firstDate)
        {
            return SeriesFail(userId, new List<string> { "Bitiş tarihi başlangıç gününden önce olamaz." });
        }

        var expansion = RecurrenceExpander.Expand(dto.Start, dto.End, dto.IsAllDay, zone, recurrence.Frequency, mask, recurrence.Until);
        if (expansion.TooMany)
        {
            return SeriesFail(userId, new List<string> { "Bir seri en fazla 366 aktivite içerebilir." });
        }
        if (expansion.Occurrences.Count == 0)
        {
            return SeriesFail(userId, new List<string> { "Bu kuralla hiç aktivite oluşmuyor." });
        }

        var now = DateTimeOffset.UtcNow;
        var occurrences = expansion.Occurrences;
        var series = new ActivitySeries
        {
            UserId = userId,
            Frequency = recurrence.Frequency,
            DaysOfWeek = mask,
            StartDate = RecurrenceExpander.LocalDateOf(occurrences[0].Start, zone),
            EndDate = RecurrenceExpander.LocalDateOf(occurrences[^1].Start, zone),
            CreatedAt = now
        };

        var activities = new List<Activity>(occurrences.Count);
        foreach (var occurrence in occurrences)
        {
            var activity = new Activity { UserId = userId, CreatedAt = now, UpdatedAt = now };
            Apply(activity, dto.CategoryId, dto.Title, dto.Description, dto.Status, dto.Priority, dto.EnergyLevel,
                occurrence.Start, occurrence.End, dto.IsAllDay, dto.Location, dto.IsFlexible, dto.EstimatedBudget, null, null);

            if (dto.ReminderMinutesBefore is { } minutes)
            {
                var remindAt = occurrence.Start.AddMinutes(-minutes);
                if (remindAt > now)
                {
                    activity.Reminders.Add(new Reminder { RemindAt = remindAt, CreatedAt = now });
                }
            }
            activities.Add(activity);
        }

        await _repository.AddSeriesAsync(series, activities, cancellationToken);
        _logger.LogInformation("Aktivite serisi oluşturuldu {SeriesId} {UserId} {Count}", series.Id, userId, activities.Count);
        return ApiResponse<ActivitySeriesCreatedDto>.Created(
            new ActivitySeriesCreatedDto(series.Id, activities.Count, occurrences[0].Start, occurrences[^1].Start),
            "Aktivite serisi oluşturuldu.");
    }

    public async Task<ApiResponse<SeriesDeactivatedDto>> DeactivateSeriesFromAsync(int seriesId, DateOnly? from, CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId;
        if (from is null)
        {
            return ApiResponse<SeriesDeactivatedDto>.Fail("Doğrulama hatası.", new List<string> { "from tarihi zorunludur." });
        }

        var series = await _repository.GetSeriesForUserAsync(seriesId, userId, cancellationToken);
        if (series is null)
        {
            if (await _repository.SeriesExistsAsync(seriesId, cancellationToken))
            {
                _logger.LogWarning("Yetkisiz seri erişim denemesi {UserId} {SeriesId}", userId, seriesId);
            }
            return ApiResponse<SeriesDeactivatedDto>.NotFound($"Seri {seriesId} bulunamadı.");
        }

        // from: kullanıcının yerel gününün başlangıcı, UTC'ye çevrilmiş.
        var zone = await GetUserZoneAsync(userId, cancellationToken);
        var fromUtc = RecurrenceExpander.StartOfLocalDayUtc(from.Value, zone);

        var count = await _repository.DeactivateSeriesFromAsync(seriesId, userId, fromUtc, DateTimeOffset.UtcNow, cancellationToken);
        _logger.LogInformation("Seri pasife alındı {SeriesId} {UserId} {Count}", seriesId, userId, count);
        return ApiResponse<SeriesDeactivatedDto>.Ok(new SeriesDeactivatedDto(count), "Seri durduruldu.");
    }

    private ApiResponse<ActivitySeriesCreatedDto> SeriesFail(int userId, List<string> errors)
    {
        _logger.LogWarning("Aktivite serisi oluşturma doğrulama hatası {UserId} {@Errors}", userId, errors);
        return ApiResponse<ActivitySeriesCreatedDto>.Fail("Doğrulama hatası.", errors);
    }

    private async Task<TimeZoneInfo> GetUserZoneAsync(int userId, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken);
        return user is not null && TimeZoneInfo.TryFindSystemTimeZoneById(user.TimeZoneId, out var zone)
            ? zone
            : TimeZoneInfo.Utc;
    }

    public async Task<ApiResponse<ActivityDetailDto>> UpdateAsync(int id, ActivityUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var activity = await FindOwnedAsync(id, cancellationToken);
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
        var activity = await FindOwnedAsync(id, cancellationToken);
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

    // Sahiplik sorgunun içindedir. Boş dönerse yalnızca bu hata yolunda ExistsAsync çalışır:
    // kayıt başkasınındır -> Warning log (dışarıya yine aynı 404); kayıt hiç yok -> log yok.
    private async Task<Activity?> FindOwnedAsync(int id, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        var activity = await _repository.GetByIdForUserAsync(id, userId, cancellationToken);
        if (activity is null && await _repository.ExistsAsync(id, cancellationToken))
        {
            _logger.LogWarning("Yetkisiz erişim denemesi {UserId} {ActivityId}", userId, id);
        }
        return activity;
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
        a.Start, a.End, a.IsAllDay, a.SeriesId);

    private static ActivityDetailDto ToDetailDto(Activity a) => new(
        a.Id, a.UserId, a.CategoryId, a.Title, a.Description,
        a.Status.ToString(), a.Priority.ToString(), a.EnergyLevel.ToString(),
        a.Start, a.End, a.IsAllDay, a.Location, a.IsFlexible, a.EstimatedBudget,
        a.Rating, a.WouldRepeat, a.IsActive, a.CreatedAt, a.UpdatedAt,
        a.Series is null
            ? null
            : new SeriesSummaryDto(a.Series.Id, a.Series.Frequency.ToString(),
                RecurrenceExpander.FromMask(a.Series.DaysOfWeek), a.Series.StartDate, a.Series.EndDate));
}
