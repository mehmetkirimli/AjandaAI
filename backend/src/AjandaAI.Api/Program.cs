using System.Data.Common;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using AjandaAI.Api.Auth;
using AjandaAI.Api.Filters;
using AjandaAI.Api.Logging;
using AjandaAI.Api.Middleware;
using AjandaAI.Application.Common;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
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

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDevelopmentInfrastructure();
}

// Authentication: JWT Bearer (ADR 0018). Claim adları dönüştürülmez ("sub", "role" olduğu gibi kalır).
// ClockSkew sıfır: 15 dakikalık access token süresi dolduğu anda reddedilir (AUTH-31).
var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
builder.Services.Configure<JwtOptions>(jwtSection);
var jwtOptions = jwtSection.Get<JwtOptions>() ?? new JwtOptions();
if (Encoding.UTF8.GetByteCount(jwtOptions.SigningKey) < 32)
{
    throw new InvalidOperationException("Jwt:SigningKey tanımlı değil veya 32 byte'tan kısa.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            NameClaimType = JwtOptions.UserIdClaim,
            RoleClaimType = JwtOptions.RoleClaim
        };
    });

// Her endpoint varsayılan olarak kimlik ister; herkese açık olanlar [AllowAnonymous] alır (ADR 0019).
builder.Services.AddAuthorization(options =>
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();

// Web istemcisi (ADR 0021): e-posta linkleri frontend sayfasını gösterir; tarayıcı farklı bir
// origin'den (Vite: http://localhost:5173) istek attığı için CORS gerekir. İzinli origin'ler
// config'den okunur; tanımlı değilse hiçbir origin'e izin verilmez. Token body/header ile taşınır,
// cookie yok (ADR 0018) — bu yüzden AllowCredentials kullanılmaz.
builder.Services.Configure<FrontendOptions>(builder.Configuration.GetSection(FrontendOptions.SectionName));
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy => policy
        .WithOrigins(corsOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()));

var authRateLimit = builder.Configuration.GetSection("RateLimiting:Auth");
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(AuthRateLimit.PolicyName, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = authRateLimit.GetValue("PermitLimit", AuthRateLimit.DefaultPermitLimit),
                Window = TimeSpan.FromSeconds(
                    authRateLimit.GetValue("WindowSeconds", AuthRateLimit.DefaultWindowSeconds)),
                QueueLimit = 0
            }));
});

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
// Swagger'da "Authorize" butonu: login'den alınan access token yapıştırılıp korumalı uçlar denenir.
builder.Services.AddSwaggerGen(options =>
{
    var bearer = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "POST /api/auth/login yanıtındaki accessToken (başına 'Bearer' yazmadan).",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };
    options.AddSecurityDefinition("Bearer", bearer);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement { [bearer] = Array.Empty<string>() });
});

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

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

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
