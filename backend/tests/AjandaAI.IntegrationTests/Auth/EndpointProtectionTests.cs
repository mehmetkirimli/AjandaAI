// Genel koruma senaryoları: AUTH-31, 32, 40, 48, 49 (ADR 0018 / 0019).
// Korumalı bir gerçek endpoint henüz olmadığı için AuthApiFactory'nin eklediği ProbeController kullanılır.
// AUTH-48 endpoint listesini elle yazmaz: uygulamanın EndpointDataSource'undan toplar; [AllowAnonymous]
// olmayan her endpoint'e token'sız istek 401 almalıdır (P6'dan beri geçici [AllowAnonymous] kalmadı).

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using AjandaAI.Api.Auth;
using AjandaAI.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AjandaAI.IntegrationTests.Auth;

public class EndpointProtectionTests : AuthTestBase, IClassFixture<AuthApiFactory>
{
    private const string ProtectedPath = "/test-probe/protected";

    public EndpointProtectionTests(AuthApiFactory factory) : base(factory)
    {
    }

    private JwtOptions JwtSettings => Factory.Services.GetRequiredService<IOptions<JwtOptions>>().Value;

    private string ForgeToken(string signingKey, int userId, DateTime notBefore, DateTime expires)
    {
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)), SecurityAlgorithms.HmacSha256);
        var jwt = new JwtSecurityToken(
            JwtSettings.Issuer, JwtSettings.Audience,
            new[] { new Claim("sub", userId.ToString()), new Claim("role", "User") },
            notBefore, expires, credentials);
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    private Task<HttpResponseMessage> GetWithTokenAsync(string path, string? token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (token is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return Client.SendAsync(request);
    }

    // AUTH-40
    [Fact]
    public async Task AUTH_40_ProtectedEndpoint_WithoutToken_Returns401()
    {
        var response = await GetWithTokenAsync(ProtectedPath, null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // AUTH-40
    [Fact]
    public async Task AUTH_40_ProtectedEndpoint_WithRealLoginToken_Returns200_AndExposesUserId()
    {
        var email = NewEmail();
        var userId = await RegisterAndVerifyAsync(email);
        var (access, _) = await LoginOkAsync(email);

        var response = await GetWithTokenAsync(ProtectedPath, access);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(userId, (await ReadDataAsync(response)).GetInt32());
    }

    // AUTH-31
    [Fact]
    public async Task AUTH_31_ExpiredAccessToken_Returns401()
    {
        var now = DateTime.UtcNow;
        var key = JwtSettings.SigningKey;
        // Pozitif kontrol: aynı anahtarla, süresi dolmamış token kabul edilir; yani 401 sebebi yalnızca süredir.
        var valid = ForgeToken(key, 1, now.AddMinutes(-1), now.AddMinutes(10));
        var expired = ForgeToken(key, 1, now.AddHours(-2), now.AddHours(-1));

        Assert.Equal(HttpStatusCode.OK, (await GetWithTokenAsync(ProtectedPath, valid)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await GetWithTokenAsync(ProtectedPath, expired)).StatusCode);
    }

    // AUTH-31 (ClockSkew = 0: varsayılan 5 dakikalık tolerans olsaydı 5 saniye önce dolan token geçerdi)
    [Fact]
    public async Task AUTH_31_TokenExpiredSecondsAgo_Returns401_NoClockSkew()
    {
        var now = DateTime.UtcNow;
        var justExpired = ForgeToken(JwtSettings.SigningKey, 1, now.AddMinutes(-15), now.AddSeconds(-5));

        Assert.Equal(HttpStatusCode.Unauthorized, (await GetWithTokenAsync(ProtectedPath, justExpired)).StatusCode);
    }

    // AUTH-32
    [Fact]
    public async Task AUTH_32_TokenSignedWithOtherKey_Returns401()
    {
        var now = DateTime.UtcNow;
        var forged = ForgeToken("baska-bir-anahtar-0123456789-0123456789", 1, now.AddMinutes(-1), now.AddMinutes(10));

        var response = await GetWithTokenAsync(ProtectedPath, forged);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // AUTH-32
    [Fact]
    public async Task AUTH_32_TokenWithTamperedPayloadOrSignature_Returns401()
    {
        var email = NewEmail();
        await RegisterAndVerifyAsync(email);
        var (access, _) = await LoginOkAsync(email);
        var parts = access.Split('.');

        // İmzanın ilk karakteri değiştirilir (son karakter base64url dolgu bitleri yüzünden güvenilmez).
        var badSignature = $"{parts[0]}.{parts[1]}.{(parts[2][0] == 'A' ? 'B' : 'A')}{parts[2][1..]}";
        // Payload'daki kullanıcı Id'si değiştirilir, imza eskisi kalır.
        var payload = Encoding.UTF8.GetString(Base64UrlEncoder.DecodeBytes(parts[1])).Replace("\"sub\":\"", "\"sub\":\"9");
        var badPayload = $"{parts[0]}.{Base64UrlEncoder.Encode(payload)}.{parts[2]}";

        Assert.Equal(HttpStatusCode.OK, (await GetWithTokenAsync(ProtectedPath, access)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await GetWithTokenAsync(ProtectedPath, badSignature)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await GetWithTokenAsync(ProtectedPath, badPayload)).StatusCode);
    }

    // AUTH-48
    [Fact]
    public async Task AUTH_48_EveryEndpointWithoutAllowAnonymous_Returns401_WithoutToken()
    {
        var endpoints = Factory.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Where(e => e.Metadata.GetMetadata<IAllowAnonymous>() is null)
            .Where(e => e.Metadata.GetMetadata<HttpMethodMetadata>() is not null)
            .ToList();
        Assert.Contains(endpoints, e => e.RoutePattern.RawText == "test-probe/protected"); // toplama gerçekten çalışıyor

        var failures = new List<string>();
        foreach (var endpoint in endpoints)
        {
            var path = "/" + Regex.Replace(endpoint.RoutePattern.RawText!.TrimStart('/'), @"\{[^}]*\}", "1");
            foreach (var method in endpoint.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods)
            {
                var request = new HttpRequestMessage(new HttpMethod(method), path);
                if (method is "POST" or "PUT" or "PATCH")
                    request.Content = JsonContent.Create(new { });
                var response = await Client.SendAsync(request);
                if (response.StatusCode != HttpStatusCode.Unauthorized)
                    failures.Add($"{method} {path} -> {(int)response.StatusCode}");
            }
        }

        Assert.Empty(failures);
    }

    // AUTH-48 (allow-list: [AllowAnonymous] yalnızca AuthController'da; yanlışlıkla açılan bir uç burada yakalanır)
    [Fact]
    public void AUTH_48_AnonymousEndpoints_AreExactlyAuthControllerActions()
    {
        var anonymous = Factory.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Where(e => e.Metadata.GetMetadata<IAllowAnonymous>() is not null)
            .Where(e => e.Metadata.GetMetadata<HttpMethodMetadata>() is not null)
            .Select(e => e.RoutePattern.RawText!.TrimStart('/'))
            .Where(route => !route.StartsWith("test-probe/")) // yalnızca testte eklenen ProbeController
            .OrderBy(route => route)
            .ToList();

        Assert.Equal(new[]
        {
            "api/auth/login",
            "api/auth/logout",
            "api/auth/refresh",
            "api/auth/register",
            "api/auth/verify-email"
        }, anonymous);
    }

    // AUTH-49
    [Fact]
    public void AUTH_49_HttpCurrentUser_WithoutIdentity_ThrowsInvalidOperationException()
    {
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        var currentUser = new HttpCurrentUser(accessor);

        Assert.Throws<InvalidOperationException>(() => currentUser.UserId);
        Assert.False(currentUser.IsAdmin);
    }

    // AUTH-49
    [Fact]
    public async Task AUTH_49_UnauthenticatedCurrentUserRead_ReturnsGeneric500_NotUnauthorized()
    {
        var response = await GetWithTokenAsync("/test-probe/anonymous-current-user", null);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        var root = await ReadRootAsync(response);
        Assert.False(root.GetProperty("success").GetBoolean());
        Assert.Equal("Beklenmeyen bir hata oluştu.", root.GetProperty("message").GetString());
    }
}
