// PAT oluşturma girdisinin kuralları (ADR 0024 §1): ad zorunlu, en fazla 100 karakter. Manuel çağrılır (ADR 0010).

using AjandaAI.Application.Auth.Dtos;
using FluentValidation;

namespace AjandaAI.Application.Auth.Validators;

public class PersonalAccessTokenCreateDtoValidator : AbstractValidator<PersonalAccessTokenCreateDto>
{
    public const int NameMaxLength = 100;

    public PersonalAccessTokenCreateDtoValidator()
    {
        RuleFor(x => x.Name)
            .Must(n => !string.IsNullOrWhiteSpace(n)).WithMessage("Anahtar adı zorunludur.")
            .Must(n => n is null || n.Trim().Length <= NameMaxLength)
            .WithMessage($"Anahtar adı en fazla {NameMaxLength} karakter olabilir.");
    }
}
