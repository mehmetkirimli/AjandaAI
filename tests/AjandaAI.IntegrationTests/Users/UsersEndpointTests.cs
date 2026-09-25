// /api/users uç noktalarını gerçek HTTP + gerçek PostgreSQL üzerinden doğrular.

using System.Net;
using System.Net.Http.Json;
using AjandaAI.Domain.Entities;
using AjandaAI.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AjandaAI.IntegrationTests.Users;

public class UsersEndpointTests : IntegrationTestBase
{
    public UsersEndpointTests(AjandaApiFactory factory) : base(factory) { }

    [Fact]
    public async Task Post_ValidUser_Returns201WithLocation()
    {
        var response = await Client.PostAsJsonAsync("/api/users",
            new { Email = "gecerli@test.com", DisplayName = "Geçerli", TimeZoneId = "Europe/Istanbul" }, Json);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var id = (await ReadDataAsync(response)).GetProperty("id").GetInt32();
        Assert.Equal($"/api/users/{id}", response.Headers.Location?.OriginalString);
        Assert.True(await Factory.WithDbAsync(db => db.Users.AnyAsync(u => u.Id == id)));
    }

    [Fact]
    public async Task Post_InvalidTimeZone_Returns400()
    {
        var response = await Client.PostAsJsonAsync("/api/users",
            new { Email = "tz@test.com", DisplayName = "TZ", TimeZoneId = "Mars/Olympus" }, Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_DuplicateEmail_Returns400()
    {
        await CreateUserAsync("ayni@test.com");

        var response = await Client.PostAsJsonAsync("/api/users",
            new { Email = "ayni@test.com", DisplayName = "Tekrar", TimeZoneId = "Europe/Istanbul" }, Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DuplicateEmail_BypassingValidator_IsRejectedByUniqueIndex()
    {
        await CreateUserAsync("index@test.com");

        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => Factory.WithDbAsync(async db =>
        {
            var now = DateTimeOffset.UtcNow;
            db.Users.Add(new User
            {
                Email = "index@test.com", DisplayName = "Doğrudan", TimeZoneId = "Europe/Istanbul",
                CreatedAt = now, UpdatedAt = now
            });
            return await db.SaveChangesAsync();
        }));

        var pg = Assert.IsType<PostgresException>(ex.InnerException);
        Assert.Equal(PostgresErrorCodes.UniqueViolation, pg.SqlState);
    }

    [Fact]
    public async Task Post_SameEmailDifferentCase_Returns4xxNot500()
    {
        await CreateUserAsync("A@x.com");

        var response = await Client.PostAsJsonAsync("/api/users",
            new { Email = "a@x.com", DisplayName = "Küçük harf", TimeZoneId = "Europe/Istanbul" }, Json);

        Assert.Contains(response.StatusCode, new[] { HttpStatusCode.BadRequest, HttpStatusCode.Conflict });
    }

    [Fact]
    public async Task DifferentCaseEmail_BypassingValidator_IsRejectedByUniqueIndex()
    {
        await CreateUserAsync("Case@test.com");

        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => Factory.WithDbAsync(async db =>
        {
            var now = DateTimeOffset.UtcNow;
            db.Users.Add(new User
            {
                Email = "case@TEST.com", DisplayName = "Doğrudan", TimeZoneId = "Europe/Istanbul",
                CreatedAt = now, UpdatedAt = now
            });
            return await db.SaveChangesAsync();
        }));

        var pg = Assert.IsType<PostgresException>(ex.InnerException);
        Assert.Equal(PostgresErrorCodes.UniqueViolation, pg.SqlState);
    }

    [Fact]
    public async Task Delete_SoftDeletes_RowRemainsInactive()
    {
        var id = await CreateUserAsync();

        var response = await Client.DeleteAsync($"/api/users/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var user = await Factory.WithDbAsync(db => db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == id));
        Assert.NotNull(user);
        Assert.False(user!.IsActive);
    }

    [Fact]
    public async Task GetAll_ExcludesInactiveUsers()
    {
        var activeId = await CreateUserAsync();
        var inactiveId = await CreateUserAsync();
        await Client.DeleteAsync($"/api/users/{inactiveId}");

        var ids = (await ReadDataAsync(await Client.GetAsync("/api/users")))
            .EnumerateArray().Select(u => u.GetProperty("id").GetInt32()).ToList();

        Assert.Contains(activeId, ids);
        Assert.DoesNotContain(inactiveId, ids);
    }

    [Fact]
    public async Task GetById_ReturnsInactiveUser()
    {
        var id = await CreateUserAsync();
        await Client.DeleteAsync($"/api/users/{id}");

        var response = await Client.GetAsync($"/api/users/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False((await ReadDataAsync(response)).GetProperty("isActive").GetBoolean());
    }
}
