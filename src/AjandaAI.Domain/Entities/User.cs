// Uygulamayı kullanan kişiyi temsil eder.
// Aktivitelerin sahibidir; tüm ajanda kayıtları bir kullanıcıya bağlıdır.
// TimeZoneId, kullanıcının yerel saat dilimini tutar (örn. "Europe/Istanbul").
// Kullanıcı silinmez, IsActive = false ile pasife alınır (soft delete).

namespace AjandaAI.Domain.Entities;

public class User
{
    public int Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string TimeZoneId { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<Activity> Activities { get; set; } = new List<Activity>();
}
