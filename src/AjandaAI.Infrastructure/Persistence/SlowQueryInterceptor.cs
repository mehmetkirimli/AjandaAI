// EF Core'un çalıştırdığı komutları izler; appsettings'teki Observability:SlowQueryThresholdMs
// (varsayılan 500ms) eşiğini aşan sorguları "SlowQuery" etiketiyle Warning seviyesinde loglar.
// AddInfrastructure içinde AddDbContext'e AddInterceptors ile kaydedilir.

using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AjandaAI.Infrastructure.Persistence;

public class SlowQueryInterceptor : DbCommandInterceptor
{
    private const int DefaultThresholdMs = 500;

    private readonly ILogger<SlowQueryInterceptor> _logger;
    private readonly int _thresholdMs;

    public SlowQueryInterceptor(ILogger<SlowQueryInterceptor> logger, IConfiguration configuration)
    {
        _logger = logger;

        // Microsoft.Extensions.Configuration.Binder paketine (GetValue<T>) bağımlılık
        // eklememek için değer elle okunur; Infrastructure.csproj'a dokunulmaz.
        var rawValue = configuration["Observability:SlowQueryThresholdMs"];
        _thresholdMs = int.TryParse(rawValue, out var configuredThreshold)
            ? configuredThreshold
            : DefaultThresholdMs;
    }

    public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
    {
        LogIfSlow(command, eventData);
        return base.ReaderExecuted(command, eventData, result);
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        LogIfSlow(command, eventData);
        return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result)
    {
        LogIfSlow(command, eventData);
        return base.NonQueryExecuted(command, eventData, result);
    }

    public override ValueTask<int> NonQueryExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        LogIfSlow(command, eventData);
        return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override object? ScalarExecuted(DbCommand command, CommandExecutedEventData eventData, object? result)
    {
        LogIfSlow(command, eventData);
        return base.ScalarExecuted(command, eventData, result);
    }

    public override ValueTask<object?> ScalarExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        object? result,
        CancellationToken cancellationToken = default)
    {
        LogIfSlow(command, eventData);
        return base.ScalarExecutedAsync(command, eventData, result, cancellationToken);
    }

    private void LogIfSlow(DbCommand command, CommandExecutedEventData eventData)
    {
        var elapsedMs = eventData.Duration.TotalMilliseconds;
        if (elapsedMs < _thresholdMs)
        {
            return;
        }

        _logger.LogWarning(
            "SlowQuery: {ElapsedMs}ms (eşik {ThresholdMs}ms) - {CommandText}",
            elapsedMs, _thresholdMs, command.CommandText);
    }
}
