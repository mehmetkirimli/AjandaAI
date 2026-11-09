// Geçmiş tarih kuralı (ADR 0024 §2b): aktivitenin başlangıcı en fazla MaxDaysInPast gün geçmişte olabilir.
// "Gün" sınırları kullanıcının saat dilimine göredir. Oluşturmada ve seride her zaman, güncellemede yalnızca
// başlangıç değişiyorsa uygulanır (ActivityService). Web, sürükle-bırak ve Claude araçları aynı kuralı paylaşır.

namespace AjandaAI.Application.Activities;

public static class PastStartRule
{
    public const int MaxDaysInPast = 30;

    public static readonly string Message = $"Başlangıç en fazla {MaxDaysInPast} gün öncesi olabilir.";

    public static bool IsAllowed(DateTimeOffset start, DateTimeOffset now, TimeZoneInfo zone) =>
        RecurrenceExpander.LocalDateOf(start, zone) >= RecurrenceExpander.LocalDateOf(now, zone).AddDays(-MaxDaysInPast);
}
