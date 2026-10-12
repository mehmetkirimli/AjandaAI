// Tekrarlayan aktivitelerin kuralını saklayan seri kaydıdır (ADR 0022).
// Her oluşum bağımsız bir Activity'dir ve SeriesId ile bu kayda bağlanır.
// DaysOfWeek bit maskesidir: Pzt=1, Sal=2, Çar=4, Per=8, Cum=16, Cmt=32, Paz=64.
// Weekly dışındaki sıklıklarda 0 olabilir.
// StartDate/EndDate saat dilimi taşımayan, kullanıcının yerel takvim günüdür; bu yüzden
// DateOnly (PostgreSQL date) kullanılır. ADR 0004'ün DateTimeOffset kuralının bilinçli istisnasıdır.

using AjandaAI.Domain.Enums;

namespace AjandaAI.Domain.Entities;

public class ActivitySeries
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public RecurrenceFrequency Frequency { get; set; }

    public int DaysOfWeek { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public User User { get; set; } = null!;
}
