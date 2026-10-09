// Register ve login için IP bazlı rate limit politikası (ADR 0018 Karar 9).
// Kullanım: controller action'ına [EnableRateLimiting(AuthRateLimit.PolicyName)].
// Sınırlar "RateLimiting:Auth" bölümünden okunur; yoksa aşağıdaki varsayılanlar geçerlidir.
// Bellek içidir: birden fazla sunucuda ortak sayaç gerekir (ADR 0018, bilinen kısıt).

namespace AjandaAI.Api.Auth;

public static class AuthRateLimit
{
    public const string PolicyName = "auth";

    public const int DefaultPermitLimit = 10;

    public const int DefaultWindowSeconds = 60;
}
