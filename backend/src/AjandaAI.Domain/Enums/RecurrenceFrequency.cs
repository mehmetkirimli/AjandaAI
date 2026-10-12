// Aktivite serisinin tekrar sıklığıdır (ADR 0022).
// Veritabanında string olarak saklanır (ADR 0002).

namespace AjandaAI.Domain.Enums;

public enum RecurrenceFrequency
{
    Daily,
    Weekdays,
    Weekends,
    Weekly,
    Monthly
}
