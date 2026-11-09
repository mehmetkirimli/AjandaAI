// Kullanıcının MCP için kişisel erişim anahtarı (ADR 0024 §1).
// Düz metin değil SHA-256 hash'i saklanır; Prefix yalnızca ekranda tanımak içindir.
// RevokedAt null ve ExpiresAt gelecekteyse anahtar etkindir. Kayıt silinmez, revoke edilir.

namespace AjandaAI.Domain.Entities;

public class PersonalAccessToken
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string TokenHash { get; set; } = string.Empty;

    public string Prefix { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? LastUsedAt { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }

    public User User { get; set; } = null!;
}
