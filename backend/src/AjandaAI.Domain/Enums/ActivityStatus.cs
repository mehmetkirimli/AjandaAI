// Bir aktivitenin yaşam döngüsündeki durumunu ifade eder.
// Planned -> InProgress -> Completed akışı normal seyirdir.
// Cancelled her aşamadan geçilebilecek son durumdur.
// PostgreSQL'de string olarak saklanır.

namespace AjandaAI.Domain.Enums;

public enum ActivityStatus
{
    Planned,
    InProgress,
    Completed,
    Cancelled
}
