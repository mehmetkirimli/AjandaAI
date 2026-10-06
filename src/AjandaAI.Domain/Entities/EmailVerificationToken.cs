// E-posta doğrulama linkindeki tek kullanımlık token'ın kaydı (SHA-256 hash'i).
// UsedAt null ise kullanılmamıştır; geçerlilik süresi 12 saattir (ADR 0018 Karar 8).
// Doğrulanmamış kullanıcı hard delete edildiğinde token'ları da silinir (Cascade).

namespace AjandaAI.Domain.Entities;

public class EmailVerificationToken
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UsedAt { get; set; }

    public User User { get; set; } = null!;
}
