// Uygulamayı kullanan kişiyi temsil eder.
// Aktivitelerin sahibidir; tüm ajanda kayıtları bir kullanıcıya bağlıdır.
// TimeZoneId, kullanıcının yerel saat dilimini tutar (örn. "Europe/Istanbul").
// Kullanıcı silinmez, IsActive = false ile pasife alınır (soft delete).

using AjandaAI.Domain.Enums;

namespace AjandaAI.Domain.Entities;

public class User
{
    public int Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string TimeZoneId { get; set; } = string.Empty;

    // Şifre hash'i (PasswordHasher). Gerçek değer P2'de atanır.
    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.User;

    // null = e-posta doğrulanmamış. IsActive'ten ayrı bir alandır (ADR 0018 Karar 8).
    public DateTimeOffset? EmailConfirmedAt { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<Activity> Activities { get; set; } = new List<Activity>();
}
