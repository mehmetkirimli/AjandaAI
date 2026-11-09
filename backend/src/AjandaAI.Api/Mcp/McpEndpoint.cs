// /mcp ucunun yetkilendirme ve istek sınırı adları (ADR 0024 §2).
// Sınır anahtar başınadır (pat_id claim'i): bir anahtarın aşması başka anahtarı etkilemez (MCP-33).
// Değerler "RateLimiting:Mcp" bölümünden okunur; yoksa aşağıdaki varsayılanlar geçerlidir.

namespace AjandaAI.Api.Mcp;

public static class McpEndpoint
{
    public const string Path = "/mcp";

    public const string AuthorizationPolicy = "mcp";

    public const string RateLimitPolicy = "mcp";

    public const int DefaultPermitLimit = 30;

    public const int DefaultWindowSeconds = 60;
}
