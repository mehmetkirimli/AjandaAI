using System.Data.Common;
using System.Text.Json.Serialization;
using AjandaAI.Api.Filters;
using AjandaAI.Api.Logging;
using AjandaAI.Api.Middleware;
using AjandaAI.Application.Common;
using Microsoft.AspNetCore.Mvc;
using AjandaAI.Infrastructure;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// Serilog yapılandırması tamamen appsettings*.json'daki "Serilog" bölümünden okunur;
// burada sink/seviye hardcode edilmez. Elasticsearch gibi bir sink'e geçiş yalnızca
// config değişikliği (appsettings + paket referansı) gerektirir.
// Destructuring policy, {@Nesne} loglarındaki kişisel verileri maskeleyen güvenlik ağıdır.
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Destructure.With<SensitiveDataDestructuringPolicy>());

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers(options =>
        options.Filters.Add<ApiResponseFilter>())
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
    .ConfigureApiBehaviorOptions(options =>
        // Model-binding hataları da ProblemDetails yerine ApiResponse formatında döner.
        // Development dışında ham mesajlar iç tip adı sızdırdığı için alan bazlı genel mesaj üretilir.
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = builder.Environment.IsDevelopment()
                ? context.ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => string.IsNullOrEmpty(e.ErrorMessage) ? "Geçersiz istek gövdesi." : e.ErrorMessage)
                    .ToList()
                : context.ModelState
                    .Where(kv => kv.Value!.Errors.Count > 0)
                    .Select(kv => ToFieldMessage(kv.Key, context.ActionDescriptor.Parameters.Select(p => p.Name)))
                    .Distinct()
                    .ToList();
            return new BadRequestObjectResult(ApiResponse<object>.Fail("Doğrulama hatası.", errors));
        });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Başlangıçta ortam ve veritabanı adı loglanır; connection string (şifre) loglanmaz.
var dbName = new DbConnectionStringBuilder
{
    ConnectionString = app.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty
}.TryGetValue("Database", out var database) ? database : "(tanımsız)";
app.Logger.LogInformation("Ortam: {EnvironmentName}, Veritabanı: {DatabaseName}",
    app.Environment.EnvironmentName, dbName);

// HTTP request logging: method, path, status code, süre (ms) Serilog tarafından otomatik
// eklenir; ClientIp ve RequestId EnrichDiagnosticContext ile eklenir. /health ve /swagger
// istekleri GetLevel ile Verbose'a düşürülüp gürültü azaltılır (MinimumLevel altında kalır).
app.UseSerilogRequestLogging(options =>
{
    options.GetLevel = (httpContext, elapsed, ex) =>
    {
        if (ex is not null)
        {
            return LogEventLevel.Error;
        }

        var path = httpContext.Request.Path;
        return path.StartsWithSegments("/health") || path.StartsWithSegments("/swagger")
            ? LogEventLevel.Verbose
            : LogEventLevel.Information;
    };

    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("ClientIp", httpContext.Connection.RemoteIpAddress?.ToString());
        diagnosticContext.Set("RequestId", httpContext.TraceIdentifier);
    };
});

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();

// ModelState anahtarından alan adını çıkarır: "$.email" / "email" -> "email alanı geçersiz."
// Anahtar boşsa, kök ("$") ise veya action parametre adıysa (örn. "dto") genel mesaj döner.
static string ToFieldMessage(string key, IEnumerable<string> parameterNames)
{
    var field = key.StartsWith("$") ? key.TrimStart('$', '.') : key;
    return string.IsNullOrWhiteSpace(field) || parameterNames.Contains(field, StringComparer.OrdinalIgnoreCase)
        ? "Geçersiz istek gövdesi."
        : $"{field} alanı geçersiz.";
}

// WebApplicationFactory<Program> için top-level Program sınıfını test projelerine açar.
public partial class Program { }
