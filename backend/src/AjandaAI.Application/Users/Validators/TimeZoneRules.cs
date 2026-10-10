// User validator'larının ortak saat dilimi kontrolüdür.
// Kimlik, çalışılan sistemin saat dilimi veritabanında bulunmalıdır (örn. "Europe/Istanbul").

namespace AjandaAI.Application.Users.Validators;

internal static class TimeZoneRules
{
    public static bool IsValid(string timeZoneId) =>
        TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out _);
}
