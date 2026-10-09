// Auth test sınıflarının ortak yardımcıları: kayıt, doğrulama, giriş, DB okuma/güncelleme.
// Her test benzersiz e-posta kullanır (sınıf içinde veritabanı paylaşılır). Fixture'ı somut sınıflar
// IClassFixture<AuthApiFactory> ile bildirir. Varsayılan şifre yaygın liste ve kişisel bilgi kurallarına takılmaz.

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AjandaAI.Domain.Entities;
using AjandaAI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AjandaAI.IntegrationTests.Auth;

public abstract class AuthTestBase
{
    protected const string DefaultPassword = "mavi-kayik-sahilde-77";
    protected const string DefaultDisplayName = "Deneme Hesabı";

    protected static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    protected AuthApiFactory Factory { get; }

    protected HttpClient Client { get; }

    protected AuthTestBase(AuthApiFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }

    protected static string NewEmail() => $"u{Guid.NewGuid():N}@test.com";

    protected Task<HttpResponseMessage> RegisterAsync(
        string email, string password = DefaultPassword, string displayName = DefaultDisplayName) =>
        Client.PostAsJsonAsync("/api/auth/register", new
        {
            Email = email,
            DisplayName = displayName,
            TimeZoneId = "Europe/Istanbul",
            Password = password
        }, Json);

    protected Task<HttpResponseMessage> VerifyAsync(string token) =>
        Client.PostAsJsonAsync("/api/auth/verify-email", new { Token = token }, Json);

    protected Task<HttpResponseMessage> LoginAsync(string email, string password = DefaultPassword) =>
        Client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = password }, Json);

    protected Task<HttpResponseMessage> RefreshAsync(string refreshToken) =>
        Client.PostAsJsonAsync("/api/auth/refresh", new { RefreshToken = refreshToken }, Json);

    protected Task<HttpResponseMessage> LogoutAsync(string refreshToken) =>
        Client.PostAsJsonAsync("/api/auth/logout", new { RefreshToken = refreshToken }, Json);

    /// <summary>Kayıt + doğrulama maili + doğrulama linki; doğrulanmış kullanıcının Id'sini döner.</summary>
    protected async Task<int> RegisterAndVerifyAsync(string email, string password = DefaultPassword,
        string displayName = DefaultDisplayName)
    {
        var register = await RegisterAsync(email, password, displayName);
        Assert.Equal(HttpStatusCode.Accepted, register.StatusCode);
        var mail = await Factory.Emails.WaitForAsync(EmailKind.Verification, email,
            nth: Math.Max(1, Factory.Emails.Count(EmailKind.Verification, email)));
        var verify = await VerifyAsync(mail.Token!);
        Assert.Equal(HttpStatusCode.OK, verify.StatusCode);
        return (await GetUserAsync(email)).Id;
    }

    protected async Task<(string AccessToken, string RefreshToken)> LoginOkAsync(string email, string password = DefaultPassword)
    {
        var response = await LoginAsync(email, password);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await ReadTokensAsync(response);
    }

    protected static async Task<(string AccessToken, string RefreshToken)> ReadTokensAsync(HttpResponseMessage response)
    {
        var data = await ReadDataAsync(response);
        return (data.GetProperty("accessToken").GetString()!, data.GetProperty("refreshToken").GetString()!);
    }

    protected static async Task<JsonElement> ReadDataAsync(HttpResponseMessage response)
    {
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("data").Clone();
    }

    protected static async Task<JsonElement> ReadRootAsync(HttpResponseMessage response)
    {
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.Clone();
    }

    protected Task<User> GetUserAsync(string email)
    {
        var normalized = email.ToLowerInvariant();
        return Factory.WithDbAsync(db => db.Users.AsNoTracking().SingleAsync(u => u.Email.ToLower() == normalized));
    }

    protected Task<User?> FindUserAsync(string email)
    {
        var normalized = email.ToLowerInvariant();
        return Factory.WithDbAsync(db => db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Email.ToLower() == normalized));
    }

    protected Task<int> CountUsersAsync(string email)
    {
        var normalized = email.ToLowerInvariant();
        return Factory.WithDbAsync(db => db.Users.CountAsync(u => u.Email.ToLower() == normalized));
    }

    protected Task<int> UpdateDbAsync(Func<AppDbContext, Task<int>> action) => Factory.WithDbAsync(action);

    protected Task<List<RefreshToken>> GetRefreshTokensAsync(int userId) =>
        Factory.WithDbAsync(db => db.RefreshTokens.AsNoTracking().Where(t => t.UserId == userId).OrderBy(t => t.Id).ToListAsync());

    protected Task<RefreshToken> GetRefreshTokenByRawAsync(string rawToken)
    {
        var hash = Sha256Hex(rawToken);
        return Factory.WithDbAsync(db => db.RefreshTokens.AsNoTracking().SingleAsync(t => t.TokenHash == hash));
    }

    protected static string Sha256Hex(string value) =>
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value)))
            .ToLowerInvariant();
}
