// Web istemcisi için backend ekleri (ADR 0021, F0): GET /api/auth/me ve CORS.
// CORS origin'leri Program.cs'te build sırasında okunduğu için UseSetting ile (host config) verilir.

using System.Net;
using System.Net.Http.Headers;
using AjandaAI.Domain.Enums;
using AjandaAI.IntegrationTests.Auth;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace AjandaAI.IntegrationTests.Web;

public class CorsAuthApiFactory : AuthApiFactory
{
    public const string AllowedOrigin = "http://localhost:5173";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Cors:AllowedOrigins:0", AllowedOrigin);
        base.ConfigureWebHost(builder);
    }
}

public class FrontendSupportTests : AuthTestBase, IClassFixture<CorsAuthApiFactory>
{
    public FrontendSupportTests(CorsAuthApiFactory factory) : base(factory) { }

    private Task<HttpResponseMessage> GetMeAsync(string? token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        if (token is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return Client.SendAsync(request);
    }

    [Fact]
    public async Task Me_WithToken_ReturnsCurrentUser_WithoutSecrets()
    {
        var email = NewEmail();
        var id = await RegisterAndVerifyAsync(email);
        var (access, _) = await LoginOkAsync(email);

        var response = await GetMeAsync(access);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var me = await ReadDataAsync(response);
        Assert.Equal(id, me.GetProperty("id").GetInt32());
        Assert.Equal(email, me.GetProperty("email").GetString());
        Assert.Equal(DefaultDisplayName, me.GetProperty("displayName").GetString());
        Assert.Equal("Europe/Istanbul", me.GetProperty("timeZoneId").GetString());
        Assert.Equal("User", me.GetProperty("role").GetString());
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("passwordHash", body, StringComparison.OrdinalIgnoreCase);
    }

    // AUTH-40 (me korumalıdır; AuthController'daki diğer uçlar anonimdir)
    [Fact]
    public async Task Me_WithoutToken_Returns401()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await GetMeAsync(null)).StatusCode);
    }

    [Fact]
    public async Task Me_InactiveUser_WithStillValidToken_Returns401()
    {
        var email = NewEmail();
        var id = await RegisterAndVerifyAsync(email);
        var (access, _) = await LoginOkAsync(email);
        await UpdateDbAsync(db => db.Users.Where(u => u.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.IsActive, false)));

        Assert.Equal(HttpStatusCode.Unauthorized, (await GetMeAsync(access)).StatusCode);
    }

    [Fact]
    public async Task Me_ReflectsRoleFromDatabase()
    {
        var email = NewEmail();
        var id = await RegisterAndVerifyAsync(email);
        await UpdateDbAsync(db => db.Users.Where(u => u.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.Role, UserRole.Admin)));
        var (access, _) = await LoginOkAsync(email);

        Assert.Equal("Admin", (await ReadDataAsync(await GetMeAsync(access))).GetProperty("role").GetString());
    }

    [Fact]
    public async Task Cors_Preflight_FromAllowedOrigin_IsAllowed_WithAuthorizationHeader()
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/activities");
        request.Headers.Add("Origin", CorsAuthApiFactory.AllowedOrigin);
        request.Headers.Add("Access-Control-Request-Method", "GET");
        request.Headers.Add("Access-Control-Request-Headers", "authorization,content-type");

        var response = await Client.SendAsync(request);

        Assert.True(response.IsSuccessStatusCode, $"Preflight {(int)response.StatusCode} döndü");
        Assert.Equal(CorsAuthApiFactory.AllowedOrigin,
            Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
        Assert.False(response.Headers.Contains("Access-Control-Allow-Credentials"));
    }

    [Fact]
    public async Task Cors_FromUnknownOrigin_HasNoAllowOriginHeader()
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/activities");
        request.Headers.Add("Origin", "http://kotu-site.example");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        var response = await Client.SendAsync(request);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }
}
