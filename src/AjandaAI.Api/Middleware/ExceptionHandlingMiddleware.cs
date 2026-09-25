// Yakalanmamış exception'ları yakalayıp ApiResponse.Error zarfıyla HTTP 500 döner.
// Exception detayı (mesaj + stack trace) yalnızca Development ortamında errors'a eklenir;
// Production'da ham hata sızdırılmaz. Pipeline'ın en başına kaydedilir.

using AjandaAI.Application.Common;

namespace AjandaAI.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Yakalanmamış exception: {Path}", context.Request.Path);

            if (context.Response.HasStarted)
            {
                throw;
            }

            var errors = _environment.IsDevelopment()
                ? new List<string> { ex.Message, ex.StackTrace ?? string.Empty }
                : null;
            var response = ApiResponse<object>.Error("Beklenmeyen bir hata oluştu.", errors);

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(response);
        }
    }
}
