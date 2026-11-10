// /api/assistant istek sınırları (ADR 0025 §4): kullanıcı başına dakikada 10 (uç politikası) ve günde 200
// (global limiter'ın asistan bölümü). Bölüm anahtarı JWT "sub" claim'idir: bir kullanıcının aşması başkasını
// etkilemez (ASSIST-22). Değerler "RateLimiting:Assistant" bölümünden okunur. Bellek içidir (ADR 0018 kısıtı).

namespace AjandaAI.Api.Assistant;

public static class AssistantRateLimit
{
    public const string PolicyName = "assistant";

    public const string PathPrefix = "/api/assistant";

    public const int DefaultPermitLimit = 10;

    public const int DefaultWindowSeconds = 60;

    public const int DefaultDailyLimit = 200;

    public const string RejectedMessage =
        "Asistan mesaj sınırına ulaştın (dakikada 10, günde 200 mesaj). Biraz sonra tekrar dene.";
}
