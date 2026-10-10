// Auth log senaryoları: AUTH-70 (gizli değerler loglarda yok) ve AUTH-71 (e-posta maskeli), ADR 0016.
// Uygulamanın gerçek Serilog pipeline'ı kullanılır; loglar InMemoryLogSink'e düşer (Information seviyesinde).
// Tüm akış (register, verify, login, hatalı login, refresh, logout) çalıştırılıp toplanan logların
// mesajı, tüm property'leri ve exception metni taranır.

using System.Net;
using AjandaAI.Application.Common.Logging;
using Serilog.Events;

namespace AjandaAI.IntegrationTests.Auth;

public class AuthLoggingTests : AuthTestBase, IClassFixture<AuthApiFactory>
{
    public AuthLoggingTests(AuthApiFactory factory) : base(factory)
    {
    }

    private static string FullText(LogEvent e) =>
        e.RenderMessage() + "\n"
        + string.Join("\n", e.Properties.Select(p => $"{p.Key}={p.Value}")) + "\n"
        + e.Exception;

    private async Task<(string Email, string Password, string[] Secrets)> RunFullFlowAsync()
    {
        var email = NewEmail();
        const string password = "yagmurlu-gunde-yuruyus-31";

        Assert.Equal(HttpStatusCode.Accepted, (await RegisterAsync(email, password)).StatusCode);
        var verificationToken = (await Factory.Emails.WaitForAsync(EmailKind.Verification, email)).Token!;
        Assert.Equal(HttpStatusCode.OK, (await VerifyAsync(verificationToken)).StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(email, "yanlis-sifre-denemesi-1")).StatusCode);
        var (access1, refresh1) = await LoginOkAsync(email, password);
        var (access2, refresh2) = await ReadTokensAsync(await RefreshAsync(refresh1));
        Assert.Equal(HttpStatusCode.Unauthorized, (await RefreshAsync(refresh1)).StatusCode); // yeniden kullanım logu
        await LogoutAsync(refresh2);

        return (email, password, new[]
        {
            password, "yanlis-sifre-denemesi-1", verificationToken, access1, refresh1, access2, refresh2
        });
    }

    [Fact]
    public async Task AUTH_70_AuthLogs_DoNotContainPasswordsOrTokens()
    {
        var (_, _, secrets) = await RunFullFlowAsync();

        var texts = Factory.Logs.Events.Select(FullText).ToList();

        Assert.Contains(texts, t => t.Contains("Giriş başarılı")); // log gerçekten toplandı (boş doğrulama olmasın)
        foreach (var secret in secrets)
            Assert.DoesNotContain(texts, t => t.Contains(secret));
    }

    [Fact]
    public async Task AUTH_71_AuthLogs_ContainMaskedEmail_NeverTheRawEmail()
    {
        var (email, _, _) = await RunFullFlowAsync();
        var local = email[..email.IndexOf('@')];

        var texts = Factory.Logs.Events.Select(FullText).ToList();

        Assert.Contains(texts, t => t.Contains(MaskingHelper.MaskEmail(email)));
        Assert.DoesNotContain(texts, t => t.Contains(email));
        Assert.DoesNotContain(texts, t => t.Contains(local));
    }
}
