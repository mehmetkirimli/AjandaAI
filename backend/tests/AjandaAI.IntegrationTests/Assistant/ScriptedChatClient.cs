// Testlerde gerçek Gemini yerine geçen sahte IChatClient (ADR 0025 test yaklaşımı). Senaryo (Script) her model
// turunda mesajlara bakıp ya "şu aracı şu girdiyle çağır" (FunctionCallContent) ya da metin döner.
// Gerçek araç kodu ve PostgreSQL çalışır; istekler (mesajlar + sunulan araçlar) incelemek için kaydedilir.

using System.Collections.Concurrent;
using System.Text.Json;
using AjandaAI.Application.Assistant;
using Microsoft.Extensions.AI;

namespace AjandaAI.IntegrationTests.Assistant;

public sealed record ModelRequest(IReadOnlyList<ChatMessage> Messages, IReadOnlyList<string> ToolNames);

public sealed class ScriptedChatClient : IChatClient
{
    public Func<IReadOnlyList<ChatMessage>, ChatResponse> Script { get; set; } = _ => Text("Merhaba.");

    public ConcurrentQueue<ModelRequest> Requests { get; } = new();

    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var list = messages.ToList();
        Requests.Enqueue(new ModelRequest(list, options?.Tools?.Select(t => t.Name).ToList() ?? new List<string>()));
        return Task.FromResult(Script(list));
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages,
        ChatOptions? options = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose() { }

    public static ChatResponse Text(string text) =>
        new(new ChatMessage(ChatRole.Assistant, text))
        {
            Usage = new UsageDetails { InputTokenCount = 11, OutputTokenCount = 7, TotalTokenCount = 18 }
        };

    public static ChatResponse Call(string tool, Dictionary<string, object?>? args = null) =>
        new(new ChatMessage(ChatRole.Assistant,
            [new FunctionCallContent(Guid.NewGuid().ToString("N"), tool, args ?? new Dictionary<string, object?>())]));

    /// <summary>Araç sonuçları (FunctionResultContent) JSON metni olarak.</summary>
    public static List<string> ToolResults(IReadOnlyList<ChatMessage> messages) =>
        messages.SelectMany(m => m.Contents).OfType<FunctionResultContent>()
            .Select(r => r.Result is string s ? s : JsonSerializer.Serialize(r.Result, AIJsonUtilities.DefaultOptions))
            .ToList();

    /// <summary>İlk turda aracı çağırır; araç sonucu gelince sonucu metin olarak "yanıtlar".</summary>
    public static Func<IReadOnlyList<ChatMessage>, ChatResponse> CallThenEcho(string tool, Dictionary<string, object?>? args = null) =>
        messages => ToolResults(messages).Count == 0 ? Call(tool, args) : Text(string.Join("\n", ToolResults(messages)));
}

public sealed class FakeAssistantModel(IChatClient? client) : IAssistantModel
{
    public IChatClient? Client { get; } = client;
}
