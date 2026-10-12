// Kategori sırası (ADR 0023 Karar 2-3) uçlarını gerçek HTTP + PostgreSQL üzerinden doğrular: ADMIN-01..ADMIN-10.
// Test veritabanında kategoriler koşular arasında birikir; beklenen liste her testte DB'den okunur.

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AjandaAI.Domain.Entities;
using AjandaAI.Domain.Enums;
using AjandaAI.Infrastructure.Persistence;
using AjandaAI.IntegrationTests.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace AjandaAI.IntegrationTests.Categories;

public class CategoryOrderEndpointTests : AuthTestBase, IClassFixture<AuthApiFactory>
{
    private const string MigrationBeforeSortOrder = "20261012071547_AddActivitySeries";

    public CategoryOrderEndpointTests(AuthApiFactory factory) : base(factory) { }

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

    private Task<HttpResponseMessage> PutOrderAsync(string? token, IEnumerable<int> ids) =>
        SendAsync(HttpMethod.Put, "/api/categories/order", token, new { CategoryIds = ids.ToArray() });

    private async Task<int> CreateCategoryAsync(string adminToken)
    {
        var response = await SendAsync(HttpMethod.Post, "/api/categories", adminToken, new { Name = $"Kat-{Guid.NewGuid():N}" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await ReadDataAsync(response)).GetProperty("id").GetInt32();
    }

    // Tüm kategoriler (aktif + pasif) mevcut sırayla.
    private Task<List<Category>> AllOrderedAsync() =>
        Factory.WithDbAsync(db => db.Categories.AsNoTracking()
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Id).ToListAsync());

    private async Task<List<(int Id, int SortOrder)>> SnapshotAsync() =>
        (await Factory.WithDbAsync(db => db.Categories.AsNoTracking().OrderBy(c => c.Id).ToListAsync()))
            .Select(c => (c.Id, c.SortOrder)).ToList();

    private async Task<List<(int Id, int SortOrder)>> GetListAsync(string token)
    {
        var response = await SendAsync(HttpMethod.Get, "/api/categories?pageSize=100", token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await ReadDataAsync(response)).GetProperty("items").EnumerateArray()
            .Select(i => (i.GetProperty("id").GetInt32(), i.GetProperty("sortOrder").GetInt32())).ToList();
    }

    // ADMIN-01: migration, seed dışı mevcut kayıtlar dahil SortOrder'ı Id sırasıyla 1..N yapar (geçici veritabanı).
    [Fact]
    public async Task ADMIN_01_Migration_NumbersExistingCategoriesByIdOrder()
    {
        var baseConnection = await Factory.WithDbAsync(db => Task.FromResult(db.Database.GetConnectionString()!));
        var connectionString = new NpgsqlConnectionStringBuilder(baseConnection)
        {
            Database = $"ajandaai_migration_{Guid.NewGuid():N}"
        }.ConnectionString;
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        await using var db = new AppDbContext(options);
        try
        {
            var migrator = db.GetService<IMigrator>();
            await migrator.MigrateAsync(MigrationBeforeSortOrder);
            await db.Database.ExecuteSqlRawAsync(
                "INSERT INTO categories (name, is_active, created_at) VALUES ('Ekstra-1', true, now()), ('Ekstra-2', false, now());");

            await migrator.MigrateAsync();

            var rows = await db.Categories.AsNoTracking().OrderBy(c => c.Id).ToListAsync();
            Assert.Equal(12, rows.Count);
            Assert.Equal(Enumerable.Range(1, rows.Count), rows.Select(c => c.SortOrder));
        }
        finally
        {
            await db.Database.EnsureDeletedAsync();
        }
    }

    // ADMIN-02: liste SortOrder'a göre sıralı ve yanıtta sortOrder var
    [Fact]
    public async Task ADMIN_02_GetList_IsOrderedBySortOrder_AndIncludesSortOrder()
    {
        var admin = await AdminTokenAsync();
        var all = await AllOrderedAsync();
        // Id sırasının tersi: sıralamanın Id'den değil SortOrder'dan geldiğini kanıtlar.
        Assert.Equal(HttpStatusCode.NoContent, (await PutOrderAsync(admin, all.Select(c => c.Id).Reverse())).StatusCode);

        var items = await GetListAsync(admin);

        var expected = (await AllOrderedAsync()).Where(c => c.IsActive).Take(100).Select(c => (c.Id, c.SortOrder));
        Assert.Equal(expected, items);
        Assert.Equal(items.OrderBy(i => i.SortOrder).ToList(), items);

        var byId = await SendAsync(HttpMethod.Get, $"/api/categories/{items[0].Id}", admin);
        Assert.Equal(items[0].SortOrder, (await ReadDataAsync(byId)).GetProperty("sortOrder").GetInt32());
    }

    // ADMIN-03: Admin tam listeyi yeni sırayla gönderir -> 204; GET yeni sırayı döner
    [Fact]
    public async Task ADMIN_03_Admin_ReorderFullList_Returns204_AndGetReturnsNewOrder()
    {
        var admin = await AdminTokenAsync();
        var ids = (await AllOrderedAsync()).Select(c => c.Id).ToList();
        var newOrder = ids.Skip(1).Append(ids[0]).ToList();

        var response = await PutOrderAsync(admin, newOrder);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var stored = await AllOrderedAsync();
        Assert.Equal(newOrder, stored.Select(c => c.Id));
        Assert.Equal(Enumerable.Range(1, newOrder.Count), stored.Select(c => c.SortOrder));
        var activeExpected = stored.Where(c => c.IsActive).Take(100).Select(c => c.Id);
        Assert.Equal(activeExpected, (await GetListAsync(admin)).Select(i => i.Id));
    }

    // ADMIN-04 (eksik), ADMIN-05 (bilinmeyen), ADMIN-06 (tekrarlı): 400, Türkçe mesaj, sıra değişmez
    [Theory]
    [InlineData("ADMIN-04")]
    [InlineData("ADMIN-05")]
    [InlineData("ADMIN-06")]
    public async Task ADMIN_04_05_06_InvalidList_Returns400_AndOrderUnchanged(string scenario)
    {
        var admin = await AdminTokenAsync();
        var ids = (await AllOrderedAsync()).Select(c => c.Id).Reverse().ToList();
        var body = scenario switch
        {
            "ADMIN-04" => ids.Skip(1).ToList(),
            "ADMIN-05" => ids.Append(int.MaxValue).ToList(),
            _ => ids.Append(ids[0]).ToList()
        };
        var before = await SnapshotAsync();

        var response = await PutOrderAsync(admin, body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var json = await ReadRootAsync(response);
        Assert.False(json.GetProperty("success").GetBoolean());
        Assert.NotEmpty(json.GetProperty("errors").EnumerateArray());
        Assert.Equal(before, await SnapshotAsync());
    }

    // ADMIN-07: User rolü 403, token'sız 401; sıra değişmez
    [Fact]
    public async Task ADMIN_07_User_Returns403_WithoutToken_Returns401()
    {
        var user = await UserTokenAsync();
        var ids = (await AllOrderedAsync()).Select(c => c.Id).Reverse().ToList();
        var before = await SnapshotAsync();

        var forbidden = await PutOrderAsync(user, ids);
        var unauthorized = await PutOrderAsync(null, ids);

        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);
        Assert.Equal(before, await SnapshotAsync());
    }

    // ADMIN-08: yeni kategori en sona (max + 1)
    [Fact]
    public async Task ADMIN_08_Create_AppendsAtEnd()
    {
        var admin = await AdminTokenAsync();
        var max = (await AllOrderedAsync()).Max(c => c.SortOrder);

        var id = await CreateCategoryAsync(admin);

        var stored = await Factory.WithDbAsync(db => db.Categories.AsNoTracking().SingleAsync(c => c.Id == id));
        Assert.Equal(max + 1, stored.SortOrder);
    }

    // ADMIN-09: pasife alma ve yeniden aktif etme sırayı korur
    [Fact]
    public async Task ADMIN_09_DeactivateThenReactivate_KeepsSortOrder()
    {
        var admin = await AdminTokenAsync();
        var id = await CreateCategoryAsync(admin);
        var ids = (await AllOrderedAsync()).Select(c => c.Id).Where(x => x != id).Prepend(id).ToList();
        Assert.Equal(HttpStatusCode.NoContent, (await PutOrderAsync(admin, ids)).StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await SendAsync(HttpMethod.Delete, $"/api/categories/{id}", admin)).StatusCode);
        var reactivate = await SendAsync(HttpMethod.Put, $"/api/categories/{id}", admin,
            new { Name = $"Kat-{Guid.NewGuid():N}", IsActive = true });

        Assert.Equal(HttpStatusCode.OK, reactivate.StatusCode);
        Assert.Equal(1, (await ReadDataAsync(reactivate)).GetProperty("sortOrder").GetInt32());
        Assert.Equal(id, (await GetListAsync(admin))[0].Id);
    }

    // ADMIN-10: pasif kategoriler tam listede yer alır ve geçerli kabul edilir; pasifsiz liste reddedilir
    [Fact]
    public async Task ADMIN_10_FullListIncludingInactive_IsAccepted_ActiveOnly_IsRejected()
    {
        var admin = await AdminTokenAsync();
        var inactiveId = await CreateCategoryAsync(admin);
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(HttpMethod.Delete, $"/api/categories/{inactiveId}", admin)).StatusCode);
        var all = await AllOrderedAsync();

        var activeOnly = await PutOrderAsync(admin, all.Where(c => c.IsActive).Select(c => c.Id));
        var full = await PutOrderAsync(admin, all.Select(c => c.Id).Reverse());

        Assert.Equal(HttpStatusCode.BadRequest, activeOnly.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, full.StatusCode);
        var stored = await Factory.WithDbAsync(db => db.Categories.AsNoTracking().SingleAsync(c => c.Id == inactiveId));
        Assert.Equal(1, stored.SortOrder);
        Assert.False(stored.IsActive);
    }

    private async Task<List<(int Id, bool IsActive)>> GetIncludeInactiveAsync(string token)
    {
        var response = await SendAsync(HttpMethod.Get, "/api/categories?includeInactive=true&pageSize=100", token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await ReadDataAsync(response)).GetProperty("items").EnumerateArray()
            .Select(i => (i.GetProperty("id").GetInt32(), i.GetProperty("isActive").GetBoolean())).ToList();
    }

    // ADMIN-11: Admin includeInactive=true ile aktif + pasif SortOrder, Id sırasıyla; User'da parametre yok sayılır.
    [Fact]
    public async Task ADMIN_11_IncludeInactive_AdminSeesAll_UserSeesOnlyActive()
    {
        var admin = await AdminTokenAsync();
        var inactiveId = await CreateCategoryAsync(admin);
        Assert.Equal(HttpStatusCode.OK, (await SendAsync(HttpMethod.Delete, $"/api/categories/{inactiveId}", admin)).StatusCode);

        var all = await AllOrderedAsync();
        var adminList = await GetIncludeInactiveAsync(admin);
        Assert.Equal(all.Take(100).Select(c => (c.Id, c.IsActive)), adminList);

        var userList = await GetIncludeInactiveAsync(await UserTokenAsync());
        Assert.Equal(all.Where(c => c.IsActive).Take(100).Select(c => (c.Id, c.IsActive)), userList);
        Assert.DoesNotContain(userList, c => c.Id == inactiveId);
    }
}
