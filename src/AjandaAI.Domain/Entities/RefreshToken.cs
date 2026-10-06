// Kullanıcının refresh token kaydı; düz metin değil SHA-256 hash'i saklanır.
// RevokedAt null ise token aktiftir; rotation ve hırsızlık tespiti bu tarihe dayanır.
// Kayıt silinmez, revoke edilir (ADR 0018 Karar 2).

namespace AjandaAI.Domain.Entities;

public class RefreshToken
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }

    public User User { get; set; } = null!;
}
