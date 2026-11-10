// Asistan testleri için factory: IAssistantModel sahte istemciyle, dış HTTP servisleri sahte handler'la değişir.
// Alt sınıflar: anahtarsız (ASSIST-01), kapalı araç (ASSIST-14), dakika sınırı (ASSIST-22), günlük sınır (ASSIST-23).

using AjandaAI.Application.Assistant;
using AjandaAI.Infrastructure;
using AjandaAI.IntegrationTests.Auth;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AjandaAI.IntegrationTests.Assistant;

public class AssistantApiFactory : AuthApiFactory
{
    public ScriptedChatClient Model { get; } = new();

    public FakeExternalHandler External { get; } = new();

    protected virtual bool Configured => true;

    protected virtual int MinuteLimit => 100_000;

    protected virtual int DailyLimit => 100_000;

    protected virtual string[] DisabledTools => [];

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        var settings = new Dictionary<string, string?>
        {
            ["RateLimiting:Assistant:PermitLimit"] = MinuteLimit.ToString(),
            ["RateLimiting:Assistant:DailyLimit"] = DailyLimit.ToString()
        };
        for (var i = 0; i < DisabledTools.Length; i++)
            settings[$"Assistant:DisabledTools:{i}"] = DisabledTools[i];
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(settings));

        builder.ConfigureTestServices(services =>
        {
            foreach (var d in services.Where(d => d.ServiceType == typeof(IAssistantModel)).ToList())
                services.Remove(d);
            services.AddSingleton<IAssistantModel>(new FakeAssistantModel(Configured ? Model : null));
            services.AddKeyedSingleton<HttpMessageHandler>(DependencyInjection.AssistantHttpHandlerKey, External);
        });
    }
}

public class UnconfiguredAssistantApiFactory : AssistantApiFactory
{
    protected override bool Configured => false;
}

public class DisabledToolAssistantApiFactory : AssistantApiFactory
{
    protected override string[] DisabledTools => [AssistantToolNames.HavaDurumu];
}

public class MinuteLimitedAssistantApiFactory : AssistantApiFactory
{
    protected override int MinuteLimit => 10;
}

public class DailyLimitedAssistantApiFactory : AssistantApiFactory
{
    protected override int DailyLimit => 3;
}
