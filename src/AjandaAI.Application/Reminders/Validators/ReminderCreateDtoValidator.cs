// ReminderCreateDto doğrulayıcısıdır.
// ActivityId var ve aktif olmalı; RemindAt geçmişte olamaz ve bağlı Activity'nin Start'ından sonra olamaz.
// Zaman kaynağı TimeProvider'dır (testte sabitlenebilir).

using AjandaAI.Application.Activities;
using AjandaAI.Application.Reminders.Dtos;
using FluentValidation;

namespace AjandaAI.Application.Reminders.Validators;

public class ReminderCreateDtoValidator : AbstractValidator<ReminderCreateDto>
{
    public ReminderCreateDtoValidator(IActivityRepository activityRepository, TimeProvider timeProvider)
    {
        RuleFor(x => x.ActivityId)
            .MustAsync((id, ct) => activityRepository.IsActiveAsync(id, ct))
            .WithMessage("Aktivite bulunamadı veya silinmiş.");

        RuleFor(x => x.RemindAt)
            .Must(remindAt => remindAt >= timeProvider.GetUtcNow())
            .WithMessage("RemindAt geçmiş bir tarih olamaz.");

        RuleFor(x => x)
            .MustAsync(async (dto, ct) =>
            {
                var activity = await activityRepository.GetByIdAsync(dto.ActivityId, ct);
                return activity is null || dto.RemindAt <= activity.Start;
            })
            .WithMessage("RemindAt, Activity başlangıç zamanından sonra olamaz.");

        RuleFor(x => x.Note).MaximumLength(500);
    }
}
