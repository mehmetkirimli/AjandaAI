// Uygulamanın gerçek Serilog pipeline'ında (Program.cs + appsettings.Test.json) {@Dto} ile
// loglanan nesnenin maskelendiğini doğrular. Mongo'ya bağımlı değildir: Test ortamında
// Mongo sink yoktur, loglar DI'a eklenen InMemoryLogSink ile yakalanır.
// Veritabanına dokunmaz; AjandaApiFactory.InitializeAsync çağrılmaz.

using AjandaAI.Application.Users.Dtos;
using AjandaAI.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using Serilog.Events;

namespace AjandaAI.IntegrationTests.Logging;

public class SensitiveDataLoggingTests
{
    [Fact]
    public void DestructuredDto_IsMasked_InApplicationLogPipeline()
    {
        var sink = new InMemoryLogSink();
        using var factory = new AjandaApiFactory().WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services => services.AddSingleton<ILogEventSink>(sink)));

        var logger = factory.Services.GetRequiredService<ILogger<SensitiveDataLoggingTests>>();
        var dto = new UserCreateDto("perihan945@hotmail.com", "Perihan Yılmaz", "Europe/Istanbul");

        // Test ortamında minimum seviye Warning olduğu için Warning ile loglanır.
        logger.LogWarning("Maskeleme testi {@User}", dto);

        var logEvent = Assert.Single(sink.Events, e => e.MessageTemplate.Text == "Maskeleme testi {@User}");
        var user = Assert.IsType<StructureValue>(logEvent.Properties["User"]);
        var props = user.Properties.ToDictionary(p => p.Name, p => ((ScalarValue)p.Value).Value);

        Assert.Equal("per*****45@hot****.com", props["Email"]);
        Assert.Equal("P*************", props["DisplayName"]);
        Assert.Equal("Europe/Istanbul", props["TimeZoneId"]);
        Assert.DoesNotContain("perihan945", logEvent.RenderMessage());
    }
}
