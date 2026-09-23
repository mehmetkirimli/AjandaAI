// Bir aktivite için kurulmuş hatırlatıcıdır.
// RemindAt mutlak zamanı tutar; gönderim durumu IsSent/SentAt ile izlenir.
// Gönderim altyapısı v2'dedir, v1'de yalnızca kayıt tutulur.

namespace AjandaAI.Domain.Entities;

public class Reminder
{
    public int Id { get; set; }

    public int ActivityId { get; set; }

    public DateTimeOffset RemindAt { get; set; }

    public bool IsSent { get; set; }

    public DateTimeOffset? SentAt { get; set; }

    public string? Note { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Activity Activity { get; set; } = null!;
}
