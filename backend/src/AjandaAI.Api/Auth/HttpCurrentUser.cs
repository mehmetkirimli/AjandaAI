// ICurrentUser'ın HTTP implementasyonu (ADR 0019): kimliği doğrulanmış isteğin token
// claim'lerinden okur. HttpContext bir web detayı olduğu için Infrastructure'da değil Api'dedir.

using AjandaAI.Application.Common;
using AjandaAI.Domain.Enums;

namespace AjandaAI.Api.Auth;

internal sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public int UserId
    {
        get
        {
            var value = accessor.HttpContext?.User.FindFirst(JwtOptions.UserIdClaim)?.Value;
            return int.TryParse(value, out var id)
                ? id
                : throw new InvalidOperationException(
                    "Kimliksiz istek Application'a ulaştı; endpoint [AllowAnonymous] olabilir.");
        }
    }

    public bool IsAdmin =>
        accessor.HttpContext?.User.IsInRole(nameof(UserRole.Admin)) ?? false;
}
