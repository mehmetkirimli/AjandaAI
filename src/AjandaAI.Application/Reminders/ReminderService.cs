// Reminder okuma ve yazma senaryolarını yöneten uygulama servisidir.
// Yazma işlemleri validator'ı elle çağırır; hata ApiResponse.Fail olarak döner.
// Update/Delete'te kayıt yoksa null döner (controller 404'e çevirir).

using AjandaAI.Application.Common;
using AjandaAI.Application.Reminders.Dtos;
using AjandaAI.Domain.Entities;
using FluentValidation;

namespace AjandaAI.Application.Reminders;

public class ReminderService
{
    private readonly IReminderRepository _repository;
    private readonly IValidator<ReminderCreateDto> _createValidator;
    private readonly IValidator<ReminderUpdateDto> _updateValidator;
    private readonly TimeProvider _timeProvider;

    public ReminderService(
        IReminderRepository repository,
        IValidator<ReminderCreateDto> createValidator,
        IValidator<ReminderUpdateDto> updateValidator,
        TimeProvider timeProvider)
    {
        _repository = repository;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _timeProvider = timeProvider;
    }

    public async Task<IReadOnlyList<ReminderListDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var reminders = await _repository.GetAllAsync(cancellationToken);
        return reminders.Select(r => new ReminderListDto(r.Id, r.ActivityId, r.RemindAt, r.IsSent)).ToList();
    }

    public async Task<ReminderDetailDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var reminder = await _repository.GetByIdAsync(id, cancellationToken);
        return reminder is null ? null : ToDetailDto(reminder);
    }

    public async Task<ApiResponse<ReminderDetailDto>> CreateAsync(ReminderCreateDto dto, CancellationToken cancellationToken = default)
    {
        var result = await _createValidator.ValidateAsync(dto, cancellationToken);
        if (!result.IsValid)
        {
            return ApiResponse<ReminderDetailDto>.Fail("Doğrulama hatası.",
                result.Errors.Select(e => e.ErrorMessage).ToList());
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
        return ApiResponse<ReminderDetailDto>.Ok(ToDetailDto(created!), "Reminder oluşturuldu.");
    }

    public async Task<ApiResponse<ReminderDetailDto>?> UpdateAsync(int id, ReminderUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var reminder = await _repository.GetByIdAsync(id, cancellationToken);
        if (reminder is null)
        {
            return null;
        }

        var result = await _updateValidator.ValidateAsync(dto, cancellationToken);
        if (!result.IsValid)
        {
            return ApiResponse<ReminderDetailDto>.Fail("Doğrulama hatası.",
                result.Errors.Select(e => e.ErrorMessage).ToList());
        }

        reminder.ActivityId = dto.ActivityId;
        reminder.RemindAt = dto.RemindAt;
        reminder.Note = dto.Note;
        await _repository.UpdateAsync(reminder, cancellationToken);

        var updated = await _repository.GetByIdAsync(id, cancellationToken);
        return ApiResponse<ReminderDetailDto>.Ok(ToDetailDto(updated!), "Reminder güncellendi.");
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        if (await _repository.GetByIdAsync(id, cancellationToken) is null)
        {
            return false;
        }

        await _repository.DeleteAsync(id, cancellationToken);
        return true;
    }

    private static ReminderDetailDto ToDetailDto(Reminder r) => new(
        r.Id, r.ActivityId, r.Activity.Title, r.RemindAt, r.IsSent, r.SentAt, r.Note, r.CreatedAt);
}
