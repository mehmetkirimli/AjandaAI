// UserCreateDto girdi doğrulamasıdır.
// Email zorunlu, geçerli formatta ve benzersiz olmalıdır; TimeZoneId geçerli bir IANA/Windows kimliği olmalıdır.

using AjandaAI.Application.Users.Dtos;
using FluentValidation;

namespace AjandaAI.Application.Users.Validators;

public class UserCreateDtoValidator : AbstractValidator<UserCreateDto>
{
    public UserCreateDtoValidator(IUserRepository repository)
    {
        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Email zorunludur.")
            .MaximumLength(256).WithMessage("Email en fazla 256 karakter olabilir.")
            .EmailAddress().WithMessage("Email formatı geçersiz.")
            .MustAsync(async (email, ct) => !await repository.EmailExistsAsync(email, null, ct))
            .WithMessage("Bu email ile kayıtlı bir kullanıcı zaten var.");

        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("Görünen ad zorunludur.")
            .MaximumLength(100).WithMessage("Görünen ad en fazla 100 karakter olabilir.");

        RuleFor(x => x.TimeZoneId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Saat dilimi zorunludur.")
            .MaximumLength(64).WithMessage("Saat dilimi en fazla 64 karakter olabilir.")
            .Must(TimeZoneRules.IsValid).WithMessage("Saat dilimi geçersiz.");
    }
}
