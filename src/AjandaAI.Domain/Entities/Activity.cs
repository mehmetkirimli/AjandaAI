// Kullanıcının planladığı veya gerçekleştirdiği tek bir ajanda kaydıdır.
// Bir kullanıcıya ve bir kategoriye bağlıdır, birden çok hatırlatıcı taşıyabilir.
// Rating ve WouldRepeat yalnızca aktivite tamamlandıktan sonra doldurulur.

using AjandaAI.Domain.Enums;

namespace AjandaAI.Domain.Entities;

public class Activity
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int CategoryId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public ActivityStatus Status { get; set; }

    public Priority Priority { get; set; }

    public EnergyLevel EnergyLevel { get; set; }

    public DateTimeOffset Start { get; set; }

    public DateTimeOffset End { get; set; }

    public bool IsAllDay { get; set; }

    public string? Location { get; set; }

    public bool IsFlexible { get; set; }

    public decimal EstimatedBudget { get; set; }

    /// <summary>1-10 arası puan. Aktivite tamamlanmadan önce null'dır.</summary>
    public int? Rating { get; set; }

    public bool? WouldRepeat { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public User User { get; set; } = null!;

    public Category Category { get; set; } = null!;

    public ICollection<Reminder> Reminders { get; set; } = new List<Reminder>();
}
