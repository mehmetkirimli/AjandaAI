// ActivityFilterDto için FluentValidation kurallarıdır.
// Sayfalama değerleri PageRequest'te sınıra çekildiği için burada doğrulanmaz.

using AjandaAI.Application.Activities.Dtos;
using FluentValidation;

namespace AjandaAI.Application.Activities.Validators;

public class ActivityFilterDtoValidator : AbstractValidator<ActivityFilterDto>
{
    public ActivityFilterDtoValidator()
    {
        RuleFor(x => x.From)
            .LessThanOrEqualTo(x => x.To)
            .When(x => x.From.HasValue && x.To.HasValue)
            .WithMessage("Başlangıç tarihi bitişten sonra olamaz.");
    }
}
