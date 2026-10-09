// RegisterDto girdi doğrulaması: e-posta/ad/saat dilimi ve şifre kuralları (ADR 0018 Karar 10).
// E-postanın DB'de var olup olmadığı BİLEREK kontrol edilmez (hesap sızdırmama, Karar 9).
// Şifre: 10-128 karakter, karmaşıklık kuralı yok, Trim yok, yaygın liste ve kişisel bilgi reddi.

using AjandaAI.Application.Auth.Dtos;
using AjandaAI.Application.Users.Validators;
using FluentValidation;

namespace AjandaAI.Application.Auth.Validators;

public class RegisterDtoValidator : AbstractValidator<RegisterDto>
{
    public const int MinPasswordLength = 10;
    public const int MaxPasswordLength = 128;

    // Kişisel bilgi eşleşmesinde bundan kısa parçalar (örn. "a@x.com" -> "a") yok sayılır;
    // aksi halde tek harflik parçalar hemen her şifreyi reddederdi.
    private const int MinPersonalPartLength = 3;

    private const string PasswordMessage =
        "Şifreniz en az 10 karakter olmalı. Uzun bir cümle kullanabilirsiniz. Çok yaygın şifreler kabul edilmez.";

    public RegisterDtoValidator(ICommonPasswordList commonPasswords)
    {
        RuleFor(x => x.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Email zorunludur.")
            .MaximumLength(256).WithMessage("Email en fazla 256 karakter olabilir.")
            .EmailAddress().WithMessage("Email formatı geçersiz.");

        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("Görünen ad zorunludur.")
            .MaximumLength(100).WithMessage("Görünen ad en fazla 100 karakter olabilir.");

        RuleFor(x => x.TimeZoneId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Saat dilimi zorunludur.")
            .MaximumLength(64).WithMessage("Saat dilimi en fazla 64 karakter olabilir.")
            .Must(TimeZoneRules.IsValid).WithMessage("Saat dilimi geçersiz. IANA formatında olmalı, örn: Europe/Istanbul");

        // Şifre Trim() edilmez: uzunluk ve içerik kontrolü olduğu gibi gelen değer üzerindedir.
        RuleFor(x => x.Password)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(PasswordMessage)
            .MinimumLength(MinPasswordLength).WithMessage(PasswordMessage)
            .MaximumLength(MaxPasswordLength).WithMessage("Şifre en fazla 128 karakter olabilir.")
            .Must(p => !string.IsNullOrWhiteSpace(p)).WithMessage(PasswordMessage)
            .Must(p => !commonPasswords.Contains(p)).WithMessage(PasswordMessage)
            .Must((dto, p) => !ContainsPersonalInfo(p, dto.Email, dto.DisplayName))
            .WithMessage("Şifre e-postanızın ilk kısmını veya görünen adınızı içeremez.");
    }

    private static bool ContainsPersonalInfo(string password, string? email, string? displayName)
    {
        foreach (var part in PersonalParts(email, displayName))
        {
            if (password.Contains(part, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    // E-postanın "@" öncesi, görünen adın tamamı ve görünen adın kelimeleri.
    private static IEnumerable<string> PersonalParts(string? email, string? displayName)
    {
        if (!string.IsNullOrWhiteSpace(email))
        {
            var value = email.Trim();
            var at = value.IndexOf('@');
            var local = at >= 0 ? value[..at] : value;
            if (local.Length >= MinPersonalPartLength)
                yield return local;
        }

        if (!string.IsNullOrWhiteSpace(displayName))
        {
            var name = displayName.Trim();
            if (name.Length >= MinPersonalPartLength)
                yield return name;

            foreach (var word in name.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                if (word.Length >= MinPersonalPartLength)
                    yield return word;
            }
        }
    }
}
