// /api/categories uç noktalarını gerçek HTTP + gerçek PostgreSQL üzerinden, token'lı isteklerle doğrular.
// AUTH-54: yazma yalnızca Admin (User rolü 403); okuma her kimlikli kullanıcıya açık; token'sız 401.
// Admin kullanıcı: kayıt + doğrulama, DB'de Role = Admin, tekrar login (rol login'de DB'den okunur, ADR 0018).

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AjandaAI.Domain.Enums;
using AjandaAI.IntegrationTests.Auth;
using Microsoft.EntityFrameworkCore;

namespace AjandaAI.IntegrationTests.Categories;

public class CategoriesEndpointTests : AuthTestBase, IClassFixture<AuthApiFactory>
{
    public CategoriesEndpointTests(AuthApiFactory factory) : base(factory) { }

    private async Task<string> UserTokenAsync()
    {
        var email = NewEmail();
        await RegisterAndVerifyAsync(email);
        return (await LoginOkAsync(email)).AccessToken;
    }

    private async Task<string> AdminTokenAsync()
    {
        var email = NewEmail();
        var id = await RegisterAndVerifyAsync(email);
        await UpdateDbAsync(db => db.Users.Where(u => u.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.Role, UserRole.Admin)));
        return (await LoginOkAsync(email)).AccessToken;
    }

    private Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, string? token, object? body = null)
    {
        var request = new HttpRequestMessage(method, path);
        if (token is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null)
            request.Content = JsonContent.Create(body, options: Json);
        return Client.SendAsync(request);
    }

    private static string NewName() => $"Kat-{Guid.NewGuid():N}";

    private async Task<int> CreateCategoryAsync(string adminToken, string? name = null)
    {
        var response = await SendAsync(HttpMethod.Post, "/api/categories", adminToken, new { Name = name ?? NewName() });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await ReadDataAsync(response)).GetProperty("id").GetInt32();
    }

    private Task<bool> IsCategoryActiveAsync(int id) =>
        Factory.WithDbAsync(db => db.Categories.AsNoTracking().Where(c => c.Id == id).Select(c => c.IsActive).SingleAsync());

    // AUTH-54 (token'sız her uç 401)
    [Theory]
    [InlineData("GET", "/api/categories")]
    [InlineData("GET", "/api/categories/1")]
    [InlineData("POST", "/api/categories")]
    [InlineData("PUT", "/api/categories/1")]
    [InlineData("DELETE", "/api/categories/1")]
    public async Task AUTH_54_WithoutToken_Returns401(string method, string path)
    {
        var body = method is "POST" or "PUT" ? new { Name = NewName(), IsActive = true } : null;

        var response = await SendAsync(new HttpMethod(method), path, null, body);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // AUTH-54 (okuma: User rolü dahil her kimlikli kullanıcıya açık)
    [Fact]
    public async Task AUTH_54_User_CanReadListAndById_Returns200()
    {
        var token = await UserTokenAsync();

        var list = await SendAsync(HttpMethod.Get, "/api/categories?pageSize=100", token);
        var byId = await SendAsync(HttpMethod.Get, "/api/categories/1", token);

        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.NotEmpty((await ReadDataAsync(list)).GetProperty("items").EnumerateArray());
        Assert.Equal(HttpStatusCode.OK, byId.StatusCode);
        Assert.Equal(1, (await ReadDataAsync(byId)).GetProperty("id").GetInt32());
    }

    // AUTH-54 (User rolüyle yazma 403 ve veri değişmez)
    [Fact]
    public async Task AUTH_54_User_Post_Returns403_AndNothingCreated()
    {
        var userToken = await UserTokenAsync();
        var name = NewName();

        var response = await SendAsync(HttpMethod.Post, "/api/categories", userToken, new { Name = name });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.False(await Factory.WithDbAsync(db => db.Categories.AnyAsync(c => c.Name == name)));
    }

    // AUTH-54
    [Fact]
    public async Task AUTH_54_User_Put_Returns403_AndCategoryUnchanged()
    {
        var adminToken = await AdminTokenAsync();
        var userToken = await UserTokenAsync();
        var name = NewName();
        var id = await CreateCategoryAsync(adminToken, name);

        var response = await SendAsync(HttpMethod.Put, $"/api/categories/{id}", userToken,
            new { Name = NewName(), IsActive = false });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var stored = await Factory.WithDbAsync(db => db.Categories.AsNoTracking().SingleAsync(c => c.Id == id));
        Assert.Equal(name, stored.Name);
        Assert.True(stored.IsActive);
    }

    // AUTH-54
    [Fact]
    public async Task AUTH_54_User_Delete_Returns403_AndCategoryStaysActive()
    {
        var adminToken = await AdminTokenAsync();
        var userToken = await UserTokenAsync();
        var id = await CreateCategoryAsync(adminToken);

        var response = await SendAsync(HttpMethod.Delete, $"/api/categories/{id}", userToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.True(await IsCategoryActiveAsync(id));
    }

    // AUTH-54 (Admin: oluşturma)
    [Fact]
    public async Task AUTH_54_Admin_Post_Returns201_AndPersists()
    {
        var adminToken = await AdminTokenAsync();
        var name = NewName();

        var response = await SendAsync(HttpMethod.Post, "/api/categories", adminToken, new { Name = name });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var data = await ReadDataAsync(response);
        Assert.Equal(name, data.GetProperty("name").GetString());
        Assert.True(await IsCategoryActiveAsync(data.GetProperty("id").GetInt32()));
    }

    // AUTH-54 (Admin: güncelleme)
    [Fact]
    public async Task AUTH_54_Admin_Put_Returns200_AndUpdates()
    {
        var adminToken = await AdminTokenAsync();
        var id = await CreateCategoryAsync(adminToken);
        var newName = NewName();

        var response = await SendAsync(HttpMethod.Put, $"/api/categories/{id}", adminToken,
            new { Name = newName, IsActive = true });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var stored = await Factory.WithDbAsync(db => db.Categories.AsNoTracking().SingleAsync(c => c.Id == id));
        Assert.Equal(newName, stored.Name);
    }

    // AUTH-54 (Admin: pasife alma; satır silinmez)
    [Fact]
    public async Task AUTH_54_Admin_Delete_Returns200_AndDeactivatesWithoutRemovingRow()
    {
        var adminToken = await AdminTokenAsync();
        var id = await CreateCategoryAsync(adminToken);

        var response = await SendAsync(HttpMethod.Delete, $"/api/categories/{id}", adminToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(await IsCategoryActiveAsync(id));
    }

    // AUTH-54 (rol token'dan değil login'de DB'den gelir: terfiden önce alınan token Admin değildir)
    [Fact]
    public async Task AUTH_54_TokenIssuedBeforePromotion_StillForbidden_UntilRelogin()
    {
        var email = NewEmail();
        var id = await RegisterAndVerifyAsync(email);
        var (oldToken, _) = await LoginOkAsync(email);
        await UpdateDbAsync(db => db.Users.Where(u => u.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.Role, UserRole.Admin)));

        var before = await SendAsync(HttpMethod.Post, "/api/categories", oldToken, new { Name = NewName() });
        var (newToken, _) = await LoginOkAsync(email);
        var after = await SendAsync(HttpMethod.Post, "/api/categories", newToken, new { Name = NewName() });

        Assert.Equal(HttpStatusCode.Forbidden, before.StatusCode);
        Assert.Equal(HttpStatusCode.Created, after.StatusCode);
    }
}
