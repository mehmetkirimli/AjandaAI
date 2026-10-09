// "Jwt" konfigürasyon bölümü. Token üretimi (Infrastructure) ve doğrulaması (Api/Program.cs)
// aynı değerleri kullanır. SigningKey git'e girmez; Production'da environment variable
// (Jwt__SigningKey) olarak verilir (ADR 0014, ADR 0018).
// Token claim'leri: "sub" = kullanıcı Id, "role" = UserRole adı (User/Admin).

namespace AjandaAI.Application.Common;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public const string UserIdClaim = "sub";

    public const string RoleClaim = "role";

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    // HMAC-SHA256 için en az 32 byte.
    public string SigningKey { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 15;

    public int RefreshTokenDays { get; set; } = 30;
}
