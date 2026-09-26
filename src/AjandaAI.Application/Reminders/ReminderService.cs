// Reminder okuma ve yazma senaryolarını yöneten uygulama servisidir.
// Yazma işlemleri validator'ı elle çağırır; hata ApiResponse.Fail olarak döner.
// Kayıt yoksa ApiResponse.NotFound döner; HTTP status kodunu ApiResponseFilter belirler.

using AjandaAI.Application.Common;
using AjandaAI.Application.Reminders.Dtos;
using AjandaAI.Domain.Entities;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace AjandaAI.Application.Reminders;

public class ReminderService
{
    private readonly IReminderRepository _repository;
    private readonly IValidator<ReminderCreateDto> _createValidator;
    private readonly IValidator<ReminderUpdateDto> _updateValidator;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ReminderService> _logger;

    public ReminderService(
        IReminderRepository repository,
        IValidator<ReminderCreateDto> createValidator,
        IValidator<ReminderUpdateDto> updateValidator,
        TimeProvider timeProvider,
        ILogger<ReminderService> logger)
    {
        _repository = repository;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<ApiResponse<PagedResult<ReminderListDto>>> GetAllAsync(ReminderFilterDto filter, CancellationToken cancellationToken = default)
    {
        var page = await _repository.GetPagedAsync(filter, cancellationToken);
        return ApiResponse<PagedResult<ReminderListDto>>.Ok(page.Map(r => new ReminderListDto(r.Id, r.ActivityId, r.RemindAt, r.IsSent)));
    }

    public async Task<ApiResponse<ReminderDetailDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var reminder = await _repository.GetByIdAsync(id, cancellationToken);
        return reminder is null
            ? ApiResponse<ReminderDetailDto>.NotFound(NotFoundMessage(id))
            : ApiResponse<ReminderDetailDto>.Ok(ToDetailDto(reminder));
    }

    public async Task<ApiResponse<ReminderDetailDto>> CreateAsync(ReminderCreateDto dto, CancellationToken cancellationToken = default)
    {
        var result = await _createValidator.ValidateAsync(dto, cancellationToken);
        if (!result.IsValid)
        {
            var errors = result.Errors.Select(e => e.ErrorMessage).ToList();
            _logger.LogWarning("Hatırlatma oluşturma doğrulama hatası {ActivityId} {@Errors}", dto.ActivityId, errors);
            return ApiResponse<ReminderDetailDto>.Fail("Doğrulama hatası.", errors);
        }

        var reminder = new Reminder
        {
            ActivityId = dto.ActivityId,
            RemindAt = dto.RemindAt,
            Note = dto.Note,
            CreatedAt = _timeProvider.GetUtcNow()
        };
        await _repository.AddAsync(reminder, cancellationToken);

        var created = await _repository.GetByIdAsync(reminder.Id, cancellationToken);
        _logger.LogInformation("Hatırlatma oluşturuldu {ReminderId} {ActivityId}", reminder.Id, dto.ActivityId);
        return ApiResponse<ReminderDetailDto>.Created(ToDetailDto(created!), "Reminder oluşturuldu.");
    }

    public async Task<ApiResponse<ReminderDetailDto>> UpdateAsync(int id, ReminderUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var reminder = await _repository.GetByIdAsync(id, cancellationToken);
        if (reminder is null)
        {
            return ApiResponse<ReminderDetailDto>.NotFound(NotFoundMessage(id));
        }

        var result = await _updateValidator.ValidateAsync(dto, cancellationToken);
        if (!result.IsValid)
        {
            var errors = result.Errors.Select(e => e.ErrorMessage).ToList();
            _logger.LogWarning("Hatırlatma güncelleme doğrulama hatası {ReminderId} {@Errors}", id, errors);
            return ApiResponse<ReminderDetailDto>.Fail("Doğrulama hatası.", errors);
        }

        reminder.ActivityId = dto.ActivityId;
        reminder.RemindAt = dto.RemindAt;
        reminder.Note = dto.Note;
        await _repository.UpdateAsync(reminder, cancellationToken);

        var updated = await _repository.GetByIdAsync(id, cancellationToken);
        return ApiResponse<ReminderDetailDto>.Ok(ToDetailDto(updated!), "Reminder güncellendi.");
    }

    public async Task<ApiResponse<bool>> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        if (await _repository.GetByIdAsync(id, cancellationToken) is null)
        {
            return ApiResponse<bool>.NotFound(NotFoundMessage(id));
        }

        await _repository.DeleteAsync(id, cancellationToken);
        _logger.LogInformation("Hatırlatma silindi {ReminderId}", id);
        return ApiResponse<bool>.Ok(true, "Reminder silindi.");
    }

    private static string NotFoundMessage(int id) => $"Reminder {id} bulunamadı.";

    private static ReminderDetailDto ToDetailDto(Reminder r) => new(
        r.Id, r.ActivityId, r.Activity.Title, r.RemindAt, r.IsSent, r.SentAt, r.Note, r.CreatedAt);
}
