// Infrastructure katmanının composition root'a sunduğu tek giriş noktasıdır.
// Program.cs yalnızca AddInfrastructure(...) çağırır, iç sınıfları tanımaz.
// DbContext, repository'ler ve Application'daki tüm IModule'ler burada kaydedilir.

using AjandaAI.Application.Activities;
using AjandaAI.Application.Admin;
using AjandaAI.Application.Assistant;
using AjandaAI.Application.Assistant.External;
using AjandaAI.Application.Auth;
using AjandaAI.Application.Categories;
using AjandaAI.Application.Common;
using AjandaAI.Application.Reminders;
using AjandaAI.Application.Users;
using AjandaAI.Infrastructure.Assistant;
using AjandaAI.Infrastructure.Auth;
using AjandaAI.Infrastructure.Persistence;
using AjandaAI.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AjandaAI.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddSingleton<SlowQueryInterceptor>();

        services.AddDbContext<AppDbContext>((sp, options) =>
            options.UseNpgsql(connectionString)
                   .UseSnakeCaseNamingConvention()
                   .AddInterceptors(sp.GetRequiredService<SlowQueryInterceptor>()));

        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IActivityRepository, ActivityRepository>();
        services.AddScoped<IReminderRepository, ReminderRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAdminRepository, AdminRepository>();
        services.AddScoped<IAuthRepository, AuthRepository>();
        services.AddSingleton<IPasswordService, PasswordService>();
        services.AddSingleton<ITokenService, TokenService>();
        services.AddSingleton<ICommonPasswordList, CommonPasswordList>();

        // Asistan (ADR 0025): anahtar yoksa model null döner, uygulama etkilenmez.
        var assistantSection = configuration.GetSection(AssistantOptions.SectionName);
        var assistantOptions = new AssistantOptions { ApiKey = assistantSection["ApiKey"] };
        if (!string.IsNullOrWhiteSpace(assistantSection["Model"]))
            assistantOptions.Model = assistantSection["Model"]!;
        services.AddSingleton(assistantOptions);
        services.AddSingleton<IAssistantModel>(new GeminiAssistantModel(assistantOptions));
        AddAssistantTools(services);

        AddModules(services);

        return services;
    }

    /// <summary>
    /// Yalnızca Development'ta çağrılır: doğrulama linkini log'a yazan LogEmailSender (ADR 0018).
    /// Ortam kararı Program.cs'te kalır, ama Api Infrastructure'ın iç sınıfını tanımaz (ADR 0012).
    /// Production'da çağrılmaz: LogEmailSender log'a gizli link yazar (AUTH-15).
    /// </summary>
    public static IServiceCollection AddDevelopmentInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IEmailSender, LogEmailSender>();
        return services;
    }

    /// <summary>
    /// Testlerin dış HTTP servislerini (Open-Meteo, Nominatim, Nager.Date) sahte handler'la değiştirmesi için anahtar:
    /// bu anahtarla bir HttpMessageHandler kayıtlıysa asistan istemcileri onu kullanır.
    /// </summary>
    public const string AssistantHttpHandlerKey = "assistant-external-http";

    public const string AssistantUserAgent = "AjandaAI/1.0 (yerel geliştirme)";

    // Asistan araçları (ADR 0025 §3): kapalı araç listesi ("Assistant:DisabledTools") ve dış servis istemcileri.
    private static void AddAssistantTools(IServiceCollection services)
    {
        // Ayar ilk kullanımda okunur (test ortamının eklediği config de görünür).
        services.AddSingleton(sp => new AssistantToolOptions
        {
            DisabledTools = sp.GetRequiredService<IConfiguration>()
                .GetSection($"{AssistantOptions.SectionName}:DisabledTools").GetChildren()
                .Select(c => c.Value).Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v!.Trim()).ToList()
        });

        services.AddSingleton<IWeatherClient>(sp => new OpenMeteoWeatherClient(CreateHttp(sp, "https://api.open-meteo.com/")));
        services.AddSingleton<IPlaceSearchClient>(sp => new NominatimPlaceSearchClient(
            CreateHttp(sp, "https://nominatim.openstreetmap.org/"), sp.GetService<TimeProvider>() ?? TimeProvider.System));
        services.AddSingleton<IHolidayClient>(sp => new NagerHolidayClient(CreateHttp(sp, "https://date.nager.at/")));
    }

    private static HttpClient CreateHttp(IServiceProvider sp, string baseAddress)
    {
        var handler = sp.GetKeyedService<HttpMessageHandler>(AssistantHttpHandlerKey);
        var http = handler is null ? new HttpClient() : new HttpClient(handler, disposeHandler: false);
        http.BaseAddress = new Uri(baseAddress);
        http.Timeout = TimeSpan.FromSeconds(10);
        http.DefaultRequestHeaders.UserAgent.ParseAdd(AssistantUserAgent);
        return http;
    }

    private static void AddModules(IServiceCollection services)
    {
        var moduleTypes = typeof(IModule).Assembly.GetTypes()
            .Where(t => typeof(IModule).IsAssignableFrom(t) && t is { IsClass: true, IsAbstract: false });

        foreach (var type in moduleTypes)
        {
            var module = (IModule)Activator.CreateInstance(type)!;
            module.Register(services);
        }
    }
}
