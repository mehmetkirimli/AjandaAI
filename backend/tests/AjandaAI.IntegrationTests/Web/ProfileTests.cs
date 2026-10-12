// Profil uçları (ADR 0021): PUT /api/auth/me ve POST /api/auth/change-password.
// Şifre değiştirmede kritik davranışlar: yanlış mevcut şifre 401 değil 400 (istemci 401'de oturumu kapatır),
// yeni şifre kayıt kurallarına tabi, başarıda bu cihaz DAHİL tüm oturumlar kapanır (yeni şifreyle tekrar giriş).

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AjandaAI.IntegrationTests.Auth;

namespace AjandaAI.IntegrationTests.Web;

public class ProfileTests : AuthTestBase, IClassFixture<AuthApiFactory>
{
    private const string NewPassword = "yesil-dagda-sabah-yuruyusu-42";

    public ProfileTests(AuthApiFactory factory) : base(factory) { }

    private Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, string token, object body)
    {
        var request = new HttpRequestMessage(method, path) { Content = JsonContent.Create(body, options: Json) };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return Client.SendAsync(request);
    }

    private async Task<(string Email, string Access, string Refresh)> NewUserAsync()
    {
        var email = NewEmail();
        await RegisterAndVerifyAsync(email);
        var (access, refresh) = await LoginOkAsync(email);
        return (email, access, refresh);
    }

    private static async Task<List<string>> ErrorsAsync(HttpResponseMessage response) =>
        (await ReadRootAsync(response)).GetProperty("errors").EnumerateArray().Select(e => e.GetString()!).ToList();

    [Fact]
    public async Task UpdateProfile_ChangesDisplayNameAndTimeZone_EmailUnchanged()
    {
        var user = await NewUserAsync();

        var response = await SendAsync(HttpMethod.Put, "/api/auth/me", user.Access,
            new { DisplayName = "  Yeni Ad  ", TimeZoneId = "Europe/Berlin" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var me = await ReadDataAsync(response);
        Assert.Equal("Yeni Ad", me.GetProperty("displayName").GetString());
        Assert.Equal("Europe/Berlin", me.GetProperty("timeZoneId").GetString());
        Assert.Equal(user.Email, me.GetProperty("email").GetString());
        var row = await GetUserAsync(user.Email);
        Assert.Equal("Yeni Ad", row.DisplayName);
        Assert.Equal("Europe/Berlin", row.TimeZoneId);
    }

    [Fact]
    public async Task UpdateProfile_InvalidInput_Returns400_WithTurkishMessages()
    {
        var user = await NewUserAsync();

        var response = await SendAsync(HttpMethod.Put, "/api/auth/me", user.Access,
            new { DisplayName = "   ", TimeZoneId = "Mars/Olympus" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = await ErrorsAsync(response);
        Assert.Contains("Görünen ad zorunludur.", errors);
        Assert.Contains(errors, e => e.StartsWith("Saat dilimi geçersiz"));
        Assert.Equal(DefaultDisplayName, (await GetUserAsync(user.Email)).DisplayName);
    }

    [Fact]
    public async Task ChangePassword_WrongCurrent_Returns400_Not401_AndSessionsStay()
    {
        var user = await NewUserAsync();

        var response = await SendAsync(HttpMethod.Post, "/api/auth/change-password", user.Access,
            new { CurrentPassword = "yanlis-mevcut-sifre-123", NewPassword });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Mevcut şifre hatalı.", await ErrorsAsync(response));
        Assert.Equal(HttpStatusCode.OK, (await RefreshAsync(user.Refresh)).StatusCode); // oturum düşmedi
        await LoginOkAsync(user.Email); // eski şifre hâlâ geçerli
    }

    [Theory]
    [InlineData("kisa-1")]            // 10 karakterden kısa (AUTH-60 kuralı)
    [InlineData("Password1!")]        // yaygın liste (AUTH-63 kuralı)
    [InlineData(DefaultPassword)]     // mevcut şifreyle aynı
    public async Task ChangePassword_NewPasswordBreaksRules_Returns400(string newPassword)
    {
        var user = await NewUserAsync();

        var response = await SendAsync(HttpMethod.Post, "/api/auth/change-password", user.Access,
            new { CurrentPassword = DefaultPassword, NewPassword = newPassword });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotEmpty(await ErrorsAsync(response));
        await LoginOkAsync(user.Email); // şifre değişmedi
    }

    [Fact]
    public async Task ChangePassword_Success_ClosesAllSessions_IncludingThisDevice()
    {
        var user = await NewUserAsync();
        var otherDevice = (await LoginOkAsync(user.Email)).RefreshToken;

        var response = await SendAsync(HttpMethod.Post, "/api/auth/change-password", user.Access,
            new { CurrentPassword = DefaultPassword, NewPassword });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(user.Refresh)).StatusCode);   // bu cihaz
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(otherDevice)).StatusCode);    // diğer cihaz
        Assert.False((await ReadRootAsync(response)).TryGetProperty("data", out var data) && data.ValueKind == System.Text.Json.JsonValueKind.Object); // token dönmez
        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(user.Email)).StatusCode);       // eski şifre
        await LoginOkAsync(user.Email, NewPassword);                                                // yeni şifre
        Assert.DoesNotContain(NewPassword, await response.Content.ReadAsStringAsync());
    }
}
