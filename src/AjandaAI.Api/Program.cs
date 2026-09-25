using System.Data.Common;
using System.Text.Json.Serialization;
using AjandaAI.Api.Filters;
using AjandaAI.Api.Middleware;
using AjandaAI.Application.Common;
using Microsoft.AspNetCore.Mvc;
using AjandaAI.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

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
