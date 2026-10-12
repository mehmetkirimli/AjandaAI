// ProfileUpdateDto girdi doğrulaması: kayıttaki görünen ad ve saat dilimi kurallarıyla aynı.

using AjandaAI.Application.Auth.Dtos;
using AjandaAI.Application.Users.Validators;
using FluentValidation;

namespace AjandaAI.Application.Auth.Validators;

public class ProfileUpdateDtoValidator : AbstractValidator<ProfileUpdateDto>
{
    public ProfileUpdateDtoValidator()
    {
        RuleFor(x => x.DisplayName)
            .Cascade(CascadeMode.Stop)
            .Must(n => !string.IsNullOrWhiteSpace(n)).WithMessage("Görünen ad zorunludur.")
            .MaximumLength(100).WithMessage("Görünen ad en fazla 100 karakter olabilir.");

        RuleFor(x => x.TimeZoneId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Saat dilimi zorunludur.")
            .MaximumLength(64).WithMessage("Saat dilimi en fazla 64 karakter olabilir.")
            .Must(TimeZoneRules.IsValid).WithMessage("Saat dilimi geçersiz. IANA formatında olmalı, örn: Europe/Istanbul");
    }
}
