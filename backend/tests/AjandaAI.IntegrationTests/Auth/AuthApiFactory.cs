// Auth testleri için AjandaApiFactory'nin genişletilmiş hali (Test ortamı, gerçek ajandaai_test veritabanı).
// Sahte IEmailSender, sayaçlı IPasswordService, bellek içi log sink'i ve ProbeController ekler;
// rate limit varsayılan olarak çok yüksektir (testler 10/dk sınırına takılmasın), AUTH-27 alt sınıfla düşürür.

using AjandaAI.Application.Auth;
using AjandaAI.IntegrationTests.Infrastructure;
using AjandaAI.IntegrationTests.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Core;

namespace AjandaAI.IntegrationTests.Auth;

public class AuthApiFactory : AjandaApiFactory
{
    public FakeEmailSender Emails { get; } = new();

    public CountingPasswordService Passwords { get; } =
        new(new AjandaAI.Infrastructure.Auth.PasswordService());

    public InMemoryLogSink Logs { get; } = new();

    protected virtual int AuthPermitLimit => 100_000;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimiting:Auth:PermitLimit"] = AuthPermitLimit.ToString(),
                // Auth olay loglarını (Information) yakalayabilmek için; appsettings.Test.json Warning'dir.
                ["Serilog:MinimumLevel:Default"] = "Information"
            }));

        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<IEmailSender>(Emails);

            foreach (var descriptor in services.Where(d => d.ServiceType == typeof(IPasswordService)).ToList())
                services.Remove(descriptor);
            services.AddSingleton<IPasswordService>(Passwords);

            services.AddSingleton<ILogEventSink>(Logs);
            services.AddControllers().AddApplicationPart(typeof(ProbeController).Assembly);
        });
    }
}

/// <summary>AUTH-27: aynı IP'den 3 istekten sonra 429 dönen factory.</summary>
public class RateLimitedAuthApiFactory : AuthApiFactory
{
    protected override int AuthPermitLimit => 3;
}
