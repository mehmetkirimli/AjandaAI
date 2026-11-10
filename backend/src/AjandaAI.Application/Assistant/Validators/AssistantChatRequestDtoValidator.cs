// Sohbet isteği sınırları (ADR 0025 §4): mesaj ≤ 2.000 karakter (ASSIST-20), geçmiş ≤ 20 mesaj (ASSIST-21),
// geçmişte yalnızca user/assistant rolü ve boş olmayan metin.

using AjandaAI.Application.Assistant.Dtos;
using FluentValidation;

namespace AjandaAI.Application.Assistant.Validators;

public class AssistantChatRequestDtoValidator : AbstractValidator<AssistantChatRequestDto>
{
    public const int MaxMessageLength = 2000;
    public const int MaxHistory = 20;

    public AssistantChatRequestDtoValidator()
    {
        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Mesaj boş olamaz.")
            .MaximumLength(MaxMessageLength).WithMessage($"Mesaj en fazla {MaxMessageLength} karakter olabilir.");

        RuleFor(x => x.History)
            .Must(h => h is null || h.Count <= MaxHistory)
            .WithMessage($"Geçmiş en fazla {MaxHistory} mesaj olabilir.");

        RuleForEach(x => x.History).ChildRules(m =>
        {
            m.RuleFor(x => x.Role)
                .Must(r => r is "user" or "assistant").WithMessage("Geçmiş mesaj rolü user veya assistant olmalıdır.");
            m.RuleFor(x => x.Text).NotEmpty().WithMessage("Geçmiş mesaj metni boş olamaz.");
        }).When(x => x.History is { Count: <= MaxHistory });
    }
}
