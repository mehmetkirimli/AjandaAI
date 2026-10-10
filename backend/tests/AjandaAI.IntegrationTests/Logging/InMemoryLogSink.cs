// Testlerde Serilog log olaylarını bellekte toplayan basit sink'tir.
// Ek paket gerektirmez; DI'a ILogEventSink olarak kaydedilince Program.cs'teki
// ReadFrom.Services tarafından uygulamanın gerçek logger'ına bağlanır.

using System.Collections.Concurrent;
using Serilog.Core;
using Serilog.Events;

namespace AjandaAI.IntegrationTests.Logging;

public class InMemoryLogSink : ILogEventSink
{
    public ConcurrentQueue<LogEvent> Events { get; } = new();

    public void Emit(LogEvent logEvent) => Events.Enqueue(logEvent);
}
