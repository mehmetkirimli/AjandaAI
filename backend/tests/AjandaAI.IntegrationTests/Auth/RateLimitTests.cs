// AUTH-27: aynı IP'den sınırı aşan login / register denemesi 429 döner.
// RateLimitedAuthApiFactory sınırı 3'e indirir; her sınıf kendi factory'sini (kendi sayacını) alır.
// Testte RemoteIpAddress yoktur, tüm istekler aynı "unknown" bölümüne düşer.

using System.Net;

namespace AjandaAI.IntegrationTests.Auth;

public class LoginRateLimitTests : AuthTestBase, IClassFixture<RateLimitedAuthApiFactory>
{
    public LoginRateLimitTests(RateLimitedAuthApiFactory factory) : base(factory)
    {
    }

    // AUTH-27
    [Fact]
    public async Task AUTH_27_Login_ExceedingLimitFromSameIp_Returns429()
    {
        for (var i = 0; i < 3; i++)
        {
            var allowed = await LoginAsync(NewEmail());
            Assert.Equal(HttpStatusCode.Unauthorized, allowed.StatusCode); // sınır içinde: normal 401
        }

        var limited = await LoginAsync(NewEmail());

        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
    }
}

public class RegisterRateLimitTests : AuthTestBase, IClassFixture<RateLimitedAuthApiFactory>
{
    public RegisterRateLimitTests(RateLimitedAuthApiFactory factory) : base(factory)
    {
    }

    // AUTH-27
    [Fact]
    public async Task AUTH_27_Register_ExceedingLimitFromSameIp_Returns429()
    {
        for (var i = 0; i < 3; i++)
        {
            var allowed = await RegisterAsync(NewEmail());
            Assert.Equal(HttpStatusCode.Accepted, allowed.StatusCode);
        }

        var limited = await RegisterAsync(NewEmail());

        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
    }
}
