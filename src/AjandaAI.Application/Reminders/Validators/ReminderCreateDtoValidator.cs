// ReminderCreateDto doğrulayıcısıdır.
// ActivityId kullanıcının KENDİ aktif aktivitesi olmalı (AUTH-45); başkasınınki, olmayanla aynı mesajı alır.
// RemindAt geçmişte olamaz ve bağlı Activity'nin Start'ından sonra olamaz; zaman kaynağı TimeProvider'dır.

using AjandaAI.Application.Activities;
using AjandaAI.Application.Common;
using AjandaAI.Application.Reminders.Dtos;
using FluentValidation;

namespace AjandaAI.Application.Reminders.Validators;

public class ReminderCreateDtoValidator : AbstractValidator<ReminderCreateDto>
{
    public ReminderCreateDtoValidator(IActivityRepository activityRepository, ICurrentUser currentUser, TimeProvider timeProvider)
    {
        RuleFor(x => x.ActivityId)
            .MustAsync(async (id, ct) =>
            {
                var activity = await activityRepository.GetByIdForUserAsync(id, currentUser.UserId, ct);
                return activity is { IsActive: true };
            })
            .WithMessage("Aktivite bulunamadı veya silinmiş.");

        RuleFor(x => x.RemindAt)
            .Must(remindAt => remindAt >= timeProvider.GetUtcNow())
            .WithMessage("RemindAt geçmiş bir tarih olamaz.");

        RuleFor(x => x)
            .MustAsync(async (dto, ct) =>
            {
                var activity = await activityRepository.GetByIdForUserAsync(dto.ActivityId, currentUser.UserId, ct);
                return activity is null || dto.RemindAt <= activity.Start;
            })
            .WithMessage("RemindAt, Activity başlangıç zamanından sonra olamaz.");

        RuleFor(x => x.Note).MaximumLength(500);
    }
}
