// AUTH-15: Production ortamında DI'da LogEmailSender (ve genel olarak hiçbir IEmailSender) kayıtlı DEĞİLDİR.
// LogEmailSender log'a gizli doğrulama token'ı yazar; yalnızca Development'ta kayıtlı olmalıdır (ADR 0018).
// Production gerçek bir ortam gibi kurulur: Jwt anahtarı ve bağlantı dizesi ortam değişkeniyle verilir
// (Program.cs bunları en başta okur). Servis sağlayıcısı açılır, veritabanına bağlanılmaz.

using AjandaAI.Application.Auth;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AjandaAI.IntegrationTests.Auth;

public class EnvironmentRegistrationTests
{
    // AUTH-15
    [Fact]
    public void AUTH_15_ProductionEnvironment_DoesNotRegisterLogEmailSender()
    {
        var variables = new Dictionary<string, string>
        {
            ["Jwt__SigningKey"] = "production-like-signing-key-0123456789-abcdef",
            ["ConnectionStrings__DefaultConnection"] = "Host=localhost;Port=5434;Database=ajandaai_test;Username=ajandaai;Password=ajandaai"
        };
        foreach (var (key, value) in variables)
            Environment.SetEnvironmentVariable(key, value);
        try
        {
            using var factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder => builder.UseEnvironment("Production"));

            Assert.Equal("Production", factory.Services.GetRequiredService<IHostEnvironment>().EnvironmentName);
            Assert.Null(factory.Services.GetService<IEmailSender>());
        }
        finally
        {
            foreach (var key in variables.Keys)
                Environment.SetEnvironmentVariable(key, null);
        }
    }
}
