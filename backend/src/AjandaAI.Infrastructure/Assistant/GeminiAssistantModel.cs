// Gemini uygulaması (ADR 0025 §1–2). Başka sağlayıcıya geçiş yalnızca bu sınıf + ayar değişikliğidir.
using AjandaAI.Application.Assistant;
using Google.GenAI;
using Microsoft.Extensions.AI;

namespace AjandaAI.Infrastructure.Assistant;

internal sealed class GeminiAssistantModel : IAssistantModel, IDisposable
{
    private readonly Client? _client;

    public GeminiAssistantModel(AssistantOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
            return;

        _client = new Client(apiKey: options.ApiKey);
        var fallback = string.IsNullOrWhiteSpace(options.FallbackModel) || options.FallbackModel == options.Model
            ? null
            : _client.AsIChatClient(options.FallbackModel);
        Client = new BusyRetryChatClient(_client.AsIChatClient(options.Model), fallback);
    }

    public IChatClient? Client { get; }

    public void Dispose()
    {
        Client?.Dispose();
        (_client as IDisposable)?.Dispose();
    }
}
