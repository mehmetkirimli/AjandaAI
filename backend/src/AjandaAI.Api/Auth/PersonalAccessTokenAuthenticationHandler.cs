// Kişisel erişim anahtarı (PAT) kimlik doğrulaması (ADR 0024 §1). Yalnızca /mcp politikası bu şemayı ister;
// /api uçlarının varsayılan şeması JWT'dir, bu yüzden bir anahtar /api'de, bir JWT /mcp'de geçmez (MCP-16, MCP-17).
// Claim adları JWT ile aynıdır ("sub", "role"): HttpCurrentUser ve sahiplik kuralları değişmeden çalışır.
// Anahtar loglanmaz; doğrulama PersonalAccessTokenService.ValidateAsync'tedir.

using System.Security.Claims;
using System.Text.Encodings.Web;
using AjandaAI.Application.Auth;
using AjandaAI.Application.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace AjandaAI.Api.Auth;

internal sealed class PersonalAccessTokenAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    PersonalAccessTokenService tokens)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "PersonalAccessToken";

    public const string TokenIdClaim = "pat_id";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        const string prefix = "Bearer ";
        var header = Request.Headers.Authorization.ToString();
        if (!header.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.NoResult();
        }

        var identity = await tokens.ValidateAsync(header[prefix.Length..].Trim(), Context.RequestAborted);
        if (identity is null)
        {
            return AuthenticateResult.Fail("Geçersiz, iptal edilmiş veya süresi dolmuş anahtar.");
        }

        var claims = new[]
        {
            new Claim(JwtOptions.UserIdClaim, identity.UserId.ToString()),
            new Claim(JwtOptions.RoleClaim, identity.Role.ToString()),
            new Claim(TokenIdClaim, identity.TokenId.ToString())
        };
        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(claims, SchemeName, JwtOptions.UserIdClaim, JwtOptions.RoleClaim));
        return AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName));
    }
}
