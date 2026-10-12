// ActivitySeriesCreateDto için FluentValidation kurallarıdır.
// Şablon alanları ActivityCreateDtoValidator ile doğrulanır (aynı mesajlar); burada yalnızca tekrar kuralı
// ve hatırlatma kuralları vardır. Saat dilimine bağlı kurallar (until, oluşum sayısı) serviste kontrol edilir.

using AjandaAI.Application.Activities.Dtos;
using AjandaAI.Domain.Enums;
using FluentValidation;

namespace AjandaAI.Application.Activities.Validators;

public class ActivitySeriesCreateDtoValidator : AbstractValidator<ActivitySeriesCreateDto>
{
    public const int MaxReminderMinutes = 10080;

    public ActivitySeriesCreateDtoValidator(IValidator<ActivityCreateDto> templateValidator)
    {
        RuleFor(x => x).CustomAsync(async (dto, context, ct) =>
        {
            var result = await templateValidator.ValidateAsync(ToTemplate(dto), ct);
            foreach (var error in result.Errors)
            {
                context.AddFailure(error.PropertyName, error.ErrorMessage);
            }
        });

        RuleFor(x => x.Recurrence).NotNull().WithMessage("Tekrar kuralı zorunludur.");

        When(x => x.Recurrence is not null, () =>
        {
            RuleFor(x => x.Recurrence!.Frequency).IsInEnum().WithMessage("Geçersiz tekrar sıklığı.");

            RuleFor(x => x.Recurrence!.DaysOfWeek)
                .Must(d => d is { Count: > 0 })
                .When(x => x.Recurrence!.Frequency == RecurrenceFrequency.Weekly)
                .WithMessage("Haftalık tekrar için en az bir gün seçilmelidir.");

            RuleFor(x => x.Recurrence!.DaysOfWeek)
                .Must(d => d!.All(v => v is >= 1 and <= 7))
                .When(x => x.Recurrence!.DaysOfWeek is not null)
                .WithMessage("Gün değerleri 1 (Pzt) ile 7 (Paz) arasında olmalıdır.");
        });

        RuleFor(x => x.ReminderMinutesBefore)
            .InclusiveBetween(1, MaxReminderMinutes).When(x => x.ReminderMinutesBefore.HasValue)
            .WithMessage("Hatırlatma süresi 1 ile 10080 dakika arasında olmalıdır.");
    }

    public static ActivityCreateDto ToTemplate(ActivitySeriesCreateDto d) => new(
        d.CategoryId, d.Title, d.Description, d.Status, d.Priority, d.EnergyLevel,
        d.Start, d.End, d.IsAllDay, d.Location, d.IsFlexible, d.EstimatedBudget, null, null);
}
