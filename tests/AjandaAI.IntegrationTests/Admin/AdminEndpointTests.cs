// /api/admin/... uçlarını gerçek HTTP + gerçek PostgreSQL üzerinden, token'lı isteklerle doğrular.
// AUTH-39 (rol düşürmede revoke), AUTH-50 (User rolü 403), AUTH-51 (içerik alanları yok),
// AUTH-52 (aktivite moderasyonu), AUTH-53 (kullanıcı endpoint'inde admin istisnası yok).
// Admin kullanıcı: kayıt + doğrulama, DB'de Role = Admin, tekrar login (rol login'de DB'den okunur).

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AjandaAI.Domain.Entities;
using AjandaAI.Domain.Enums;
using AjandaAI.IntegrationTests.Auth;
using Microsoft.EntityFrameworkCore;

namespace AjandaAI.IntegrationTests.Admin;

public class AdminEndpointTests : AuthTestBase, IClassFixture<AuthApiFactory>
{
    public AdminEndpointTests(AuthApiFactory factory) : base(factory) { }

    private async Task<(int Id, string Email, string AccessToken, string RefreshToken)> NewUserAsync(UserRole role = UserRole.User)
    {
        var email = NewEmail();
        var id = await RegisterAndVerifyAsync(email);
        if (role == UserRole.Admin)
            await UpdateDbAsync(db => db.Users.Where(u => u.Id == id)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.Role, UserRole.Admin)));
        var (access, refresh) = await LoginOkAsync(email);
        return (id, email, access, refresh);
    }

    private Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, string token, object? body = null)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null)
            request.Content = JsonContent.Create(body, options: Json);
        return Client.SendAsync(request);
    }

    // Aktivite ve hatırlatma doğrudan DB'ye yazılır; içerik alanları ayırt edilebilir değerler taşır.
    private Task<(int ActivityId, int ReminderId)> CreateContentAsync(int userId) =>
        Factory.WithDbAsync(async db =>
        {
            var categoryId = await db.Categories.Where(c => c.IsActive).Select(c => c.Id).FirstAsync();
            var now = DateTimeOffset.UtcNow;
            var activity = new Activity
            {
                UserId = userId, CategoryId = categoryId,
                Title = "GIZLI-BASLIK", Description = "GIZLI-ACIKLAMA", Location = "GIZLI-KONUM",
                Status = ActivityStatus.Planned, Priority = Priority.Medium, EnergyLevel = EnergyLevel.Medium,
                Start = now.AddDays(1), End = now.AddDays(1).AddHours(1), CreatedAt = now, UpdatedAt = now
            };
            db.Activities.Add(activity);
            await db.SaveChangesAsync();
            var reminder = new Reminder { ActivityId = activity.Id, RemindAt = now.AddHours(20), Note = "GIZLI-NOT", CreatedAt = now };
            db.Reminders.Add(reminder);
            await db.SaveChangesAsync();
            return (activity.Id, reminder.Id);
        });

    // AUTH-50
    [Theory]
    [InlineData("GET", "/api/admin/users")]
    [InlineData("GET", "/api/admin/users/1")]
    [InlineData("POST", "/api/admin/users")]
    [InlineData("PUT", "/api/admin/users/1")]
    [InlineData("DELETE", "/api/admin/users/1")]
    [InlineData("GET", "/api/admin/activities")]
    [InlineData("GET", "/api/admin/activities/1")]
    [InlineData("DELETE", "/api/admin/activities/1")]
    [InlineData("GET", "/api/admin/reminders")]
    public async Task AUTH_50_UserRole_OnAdminEndpoints_Returns403(string method, string path)
    {
        var user = await NewUserAsync();

        var response = await SendAsync(new HttpMethod(method), path, user.AccessToken,
            method is "POST" or "PUT" ? new { } : null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // AUTH-51
    [Fact]
    public async Task AUTH_51_Admin_ReadsActivityAndReminderMetadata_WithoutContentFields()
    {
        var admin = await NewUserAsync(UserRole.Admin);
        var owner = await NewUserAsync();
        var (activityId, reminderId) = await CreateContentAsync(owner.Id);

        var single = await SendAsync(HttpMethod.Get, $"/api/admin/activities/{activityId}", admin.AccessToken);
        var list = await SendAsync(HttpMethod.Get, $"/api/admin/activities?userId={owner.Id}", admin.AccessToken);
        var reminders = await SendAsync(HttpMethod.Get, $"/api/admin/reminders?activityId={activityId}", admin.AccessToken);

        Assert.Equal(HttpStatusCode.OK, single.StatusCode);
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Equal(HttpStatusCode.OK, reminders.StatusCode);

        var singleBody = await single.Content.ReadAsStringAsync();
        var listBody = await list.Content.ReadAsStringAsync();
        var reminderBody = await reminders.Content.ReadAsStringAsync();
        foreach (var body in new[] { singleBody, listBody, reminderBody })
        {
            Assert.DoesNotContain("GIZLI", body);
            Assert.DoesNotContain("\"title\"", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("\"description\"", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("\"location\"", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("\"note\"", body, StringComparison.OrdinalIgnoreCase);
        }

        // Üstveri gerçekten dönüyor.
        var activity = await ReadDataAsync(single);
        Assert.Equal(activityId, activity.GetProperty("id").GetInt32());
        Assert.Equal(owner.Id, activity.GetProperty("userId").GetInt32());
        var listItem = Assert.Single((await ReadDataAsync(list)).GetProperty("items").EnumerateArray());
        Assert.Equal(activityId, listItem.GetProperty("id").GetInt32());
        var reminderItem = Assert.Single((await ReadDataAsync(reminders)).GetProperty("items").EnumerateArray());
        Assert.Equal(reminderId, reminderItem.GetProperty("id").GetInt32());
        Assert.Equal(owner.Id, reminderItem.GetProperty("userId").GetInt32());
        Assert.False(reminderItem.GetProperty("isSent").GetBoolean());
    }

    // AUTH-52
    [Fact]
    public async Task AUTH_52_Admin_DeactivatesActivity_IsActiveFalse_RowRemains()
    {
        var admin = await NewUserAsync(UserRole.Admin);
        var owner = await NewUserAsync();
        var (activityId, _) = await CreateContentAsync(owner.Id);

        var response = await SendAsync(HttpMethod.Delete, $"/api/admin/activities/{activityId}", admin.AccessToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var row = await Factory.WithDbAsync(db => db.Activities.AsNoTracking().SingleAsync(a => a.Id == activityId));
        Assert.False(row.IsActive);
        Assert.Equal("GIZLI-BASLIK", row.Title); // içerik değiştirilmez
        Assert.Equal(HttpStatusCode.NotFound,
            (await SendAsync(HttpMethod.Delete, "/api/admin/activities/999999", admin.AccessToken)).StatusCode);
    }

    // AUTH-53
    [Fact]
    public async Task AUTH_53_Admin_OnUserEndpoint_OtherUsersActivity_Returns404_NoAdminBypass()
    {
        var admin = await NewUserAsync(UserRole.Admin);
        var owner = await NewUserAsync();
        var (activityId, reminderId) = await CreateContentAsync(owner.Id);

        Assert.Equal(HttpStatusCode.NotFound,
            (await SendAsync(HttpMethod.Get, $"/api/activities/{activityId}", admin.AccessToken)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await SendAsync(HttpMethod.Get, $"/api/reminders/{reminderId}", admin.AccessToken)).StatusCode);
        var list = await ReadDataAsync(await SendAsync(HttpMethod.Get, "/api/activities", admin.AccessToken));
        Assert.DoesNotContain(list.GetProperty("items").EnumerateArray(), a => a.GetProperty("id").GetInt32() == activityId);
    }

    // AUTH-39
    [Fact]
    public async Task AUTH_39_DemoteAdminToUser_RevokesAllRefreshTokens()
    {
        var admin = await NewUserAsync(UserRole.Admin);
        var target = await NewUserAsync(UserRole.Admin);
        var secondSession = (await LoginOkAsync(target.Email)).RefreshToken;

        var response = await SendAsync(HttpMethod.Put, $"/api/admin/users/{target.Id}", admin.AccessToken, new
        {
            Email = target.Email, DisplayName = DefaultDisplayName, TimeZoneId = "Europe/Istanbul", Role = "User"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tokens = await GetRefreshTokensAsync(target.Id);
        Assert.Equal(2, tokens.Count);
        Assert.All(tokens, t => Assert.NotNull(t.RevokedAt));
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(target.RefreshToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(secondSession)).StatusCode);
        Assert.Equal(UserRole.User, (await GetUserAsync(target.Email)).Role);
    }

    // AUTH-39 (terfi oturumları kapatmaz; yeni rol tekrar login'de gelir)
    [Fact]
    public async Task AUTH_39_PromoteUserToAdmin_DoesNotRevokeTokens()
    {
        var admin = await NewUserAsync(UserRole.Admin);
        var target = await NewUserAsync();

        var response = await SendAsync(HttpMethod.Put, $"/api/admin/users/{target.Id}", admin.AccessToken, new
        {
            Email = target.Email, DisplayName = DefaultDisplayName, TimeZoneId = "Europe/Istanbul", Role = "Admin"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.All(await GetRefreshTokensAsync(target.Id), t => Assert.Null(t.RevokedAt));
    }

    // AUTH-39 (rol alanı eksik veya tanımsızsa 400; sessizce User sayılıp admin düşürülmez)
    [Fact]
    public async Task AUTH_39_UpdateWithMissingOrUndefinedRole_Returns400_AdminKeepsRoleAndTokens()
    {
        var admin = await NewUserAsync(UserRole.Admin);
        var target = await NewUserAsync(UserRole.Admin);

        var missing = await SendAsync(HttpMethod.Put, $"/api/admin/users/{target.Id}", admin.AccessToken, new
        {
            Email = target.Email, DisplayName = DefaultDisplayName, TimeZoneId = "Europe/Istanbul"
        });
        var undefined = await SendAsync(HttpMethod.Put, $"/api/admin/users/{target.Id}", admin.AccessToken, new
        {
            Email = target.Email, DisplayName = DefaultDisplayName, TimeZoneId = "Europe/Istanbul", Role = 7
        });

        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, undefined.StatusCode);
        Assert.Equal(UserRole.Admin, (await GetUserAsync(target.Email)).Role);
        Assert.All(await GetRefreshTokensAsync(target.Id), t => Assert.Null(t.RevokedAt));
    }

    [Fact]
    public async Task Admin_CreatesUser_VerifiedWithRole_CanLogin_PasswordRulesApply()
    {
        var admin = await NewUserAsync(UserRole.Admin);
        var email = NewEmail();

        var weak = await SendAsync(HttpMethod.Post, "/api/admin/users", admin.AccessToken, new
        {
            Email = email, DisplayName = "Yeni Kisi", TimeZoneId = "Europe/Istanbul", Password = "Password1!", Role = "Admin"
        });
        Assert.Equal(HttpStatusCode.BadRequest, weak.StatusCode);

        var created = await SendAsync(HttpMethod.Post, "/api/admin/users", admin.AccessToken, new
        {
            Email = email, DisplayName = "Yeni Kisi", TimeZoneId = "Europe/Istanbul", Password = DefaultPassword, Role = "Admin"
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var user = await GetUserAsync(email);
        Assert.Equal(UserRole.Admin, user.Role);
        Assert.NotNull(user.EmailConfirmedAt);
        Assert.DoesNotContain(DefaultPassword, await created.Content.ReadAsStringAsync());
        await LoginOkAsync(email);

        var duplicate = await SendAsync(HttpMethod.Post, "/api/admin/users", admin.AccessToken, new
        {
            Email = email.ToUpperInvariant(), DisplayName = "Baska", TimeZoneId = "Europe/Istanbul", Password = DefaultPassword, Role = "User"
        });
        Assert.Equal(HttpStatusCode.BadRequest, duplicate.StatusCode);
    }

    [Fact]
    public async Task Admin_DeactivatesUser_SoftDelete_RevokesTokens_ListFiltersByIsActive()
    {
        var admin = await NewUserAsync(UserRole.Admin);
        var target = await NewUserAsync();

        var response = await SendAsync(HttpMethod.Delete, $"/api/admin/users/{target.Id}", admin.AccessToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var row = await GetUserAsync(target.Email);
        Assert.False(row.IsActive);
        Assert.All(await GetRefreshTokensAsync(target.Id), t => Assert.NotNull(t.RevokedAt));

        var inactive = await ReadDataAsync(await SendAsync(HttpMethod.Get,
            $"/api/admin/users?isActive=false&search={target.Email}", admin.AccessToken));
        var item = Assert.Single(inactive.GetProperty("items").EnumerateArray());
        Assert.Equal(target.Id, item.GetProperty("id").GetInt32());
        Assert.False(item.TryGetProperty("passwordHash", out _));
    }
}
