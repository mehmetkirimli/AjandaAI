// Token senaryoları: AUTH-30, 33..38 (AUTH-31/32 korumalı endpoint testlerinde: EndpointProtectionTests).
// Refresh token yaşam döngüsü (rotation, yeniden kullanım tespiti, süre aşımı, logout) gerçek DB'de doğrulanır;
// süre aşımı DB'de ExpiresAt geriye çekilerek simüle edilir.

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using AjandaAI.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AjandaAI.IntegrationTests.Auth;

public class TokenTests : AuthTestBase, IClassFixture<AuthApiFactory>
{
    public TokenTests(AuthApiFactory factory) : base(factory)
    {
    }

    private async Task<(int UserId, string Email, string Access, string Refresh)> NewLoggedInUserAsync()
    {
        var email = NewEmail();
        var userId = await RegisterAndVerifyAsync(email);
        var (access, refresh) = await LoginOkAsync(email);
        return (userId, email, access, refresh);
    }

    // AUTH-30
    [Fact]
    public async Task AUTH_30_AccessToken_LivesFifteenMinutes()
    {
        var email = NewEmail();
        await RegisterAndVerifyAsync(email);
        var before = DateTimeOffset.UtcNow;

        var response = await LoginAsync(email);

        var data = await ReadDataAsync(response);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(data.GetProperty("accessToken").GetString()!);
        Assert.Equal(TimeSpan.FromMinutes(15), jwt.ValidTo - jwt.ValidFrom);
        var expiresAt = data.GetProperty("accessTokenExpiresAt").GetDateTimeOffset();
        Assert.InRange((expiresAt - before).TotalMinutes, 14.9, 15.1);
    }

    // AUTH-33
    [Fact]
    public async Task AUTH_33_Refresh_ReturnsNewTokens_AndRevokesOldRefreshToken()
    {
        var (userId, _, _, refresh) = await NewLoggedInUserAsync();

        var response = await RefreshAsync(refresh);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var (newAccess, newRefresh) = await ReadTokensAsync(response);
        Assert.False(string.IsNullOrWhiteSpace(newAccess));
        Assert.NotEqual(refresh, newRefresh);
        var old = await GetRefreshTokenByRawAsync(refresh);
        Assert.NotNull(old.RevokedAt);
        var fresh = await GetRefreshTokenByRawAsync(newRefresh);
        Assert.Null(fresh.RevokedAt);
        Assert.Equal(2, (await GetRefreshTokensAsync(userId)).Count); // eski silinmez, revoke edilir
    }

    // AUTH-34
    [Fact]
    public async Task AUTH_34_RevokedRefreshTokenReused_IsRejected_AndAllUserTokensAreRevoked()
    {
        var (userId, _, _, refresh1) = await NewLoggedInUserAsync();
        var refresh2 = (await ReadTokensAsync(await RefreshAsync(refresh1))).RefreshToken;
        // Aynı kullanıcının ikinci oturumu da iptal edilmeli.
        var otherSession = (await LoginOkAsync((await Factory.WithDbAsync(db =>
            db.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => u.Email).SingleAsync())))).RefreshToken;

        var reuse = await RefreshAsync(refresh1); // refresh1 zaten revoke edilmişti

        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);
        var tokens = await GetRefreshTokensAsync(userId);
        Assert.NotEmpty(tokens);
        Assert.All(tokens, t => Assert.NotNull(t.RevokedAt));
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(refresh2)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(otherSession)).StatusCode);
    }

    // AUTH-35
    [Fact]
    public async Task AUTH_35_Refresh_TokenOlderThan30Days_IsRejected()
    {
        var (userId, _, _, refresh) = await NewLoggedInUserAsync();
        await UpdateDbAsync(db => db.RefreshTokens.Where(t => t.UserId == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.ExpiresAt, DateTimeOffset.UtcNow.AddMinutes(-1))));

        var response = await RefreshAsync(refresh);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // AUTH-35
    [Fact]
    public async Task AUTH_35_RefreshToken_LifetimeIsThirtyDays()
    {
        var (_, _, _, refresh) = await NewLoggedInUserAsync();

        var row = await GetRefreshTokenByRawAsync(refresh);

        Assert.InRange((row.ExpiresAt - row.CreatedAt).TotalDays, 29.99, 30.01);
    }

    // AUTH-36
    [Fact]
    public async Task AUTH_36_RefreshToken_IsStoredAsSha256Hash_NotPlainText()
    {
        var (userId, _, _, refresh) = await NewLoggedInUserAsync();

        var rows = await GetRefreshTokensAsync(userId);

        var row = Assert.Single(rows);
        Assert.NotEqual(refresh, row.TokenHash);
        Assert.Equal(Sha256Hex(refresh), row.TokenHash);
    }

    // AUTH-37
    [Fact]
    public async Task AUTH_37_Logout_RevokesRefreshToken_AndNextRefreshIsRejected()
    {
        var (_, _, _, refresh) = await NewLoggedInUserAsync();

        var logout = await LogoutAsync(refresh);

        Assert.Equal(HttpStatusCode.OK, logout.StatusCode);
        Assert.NotNull((await GetRefreshTokenByRawAsync(refresh)).RevokedAt);
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(refresh)).StatusCode);
    }

    // AUTH-37
    [Fact]
    public async Task AUTH_37_Logout_UnknownToken_StillReturns200()
    {
        var response = await LogoutAsync("bilinmeyen-bir-token");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // AUTH-38
    [Fact]
    public async Task AUTH_38_Refresh_ReadsRoleFromDatabase_NotFromOldToken()
    {
        var (userId, _, access, refresh) = await NewLoggedInUserAsync();
        Assert.Equal("User", new JwtSecurityTokenHandler().ReadJwtToken(access).Claims.Single(c => c.Type == "role").Value);
        await UpdateDbAsync(db => db.Users.Where(u => u.Id == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.Role, UserRole.Admin)));

        var response = await RefreshAsync(refresh);

        var (newAccess, _) = await ReadTokensAsync(response);
        var role = new JwtSecurityTokenHandler().ReadJwtToken(newAccess).Claims.Single(c => c.Type == "role").Value;
        Assert.Equal("Admin", role);
    }
}
