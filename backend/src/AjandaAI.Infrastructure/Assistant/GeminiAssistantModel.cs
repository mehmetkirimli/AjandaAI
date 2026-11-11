// Gemini uygulaması (ADR 0025 §1–2). Başka sağlayıcıya geçiş yalnızca bu sınıf + ayar değişikliğidir.
using AjandaAI.Application.Assistant;
using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.Extensions.AI;

namespace AjandaAI.Infrastructure.Assistant;

internal sealed class GeminiAssistantModel : IAssistantModel, IDisposable
{
    private readonly Client? _client;

    public GeminiAssistantModel(AssistantOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
            return;

        // SDK varsayılanı 429/5xx için 5 deneme, 60 sn'ye kadar bekleme → kota dolunca cevap 2 dakika sürüyordu
        // (2026-11-11 kabul denemesi). Yeniden denemeyi BusyRetryChatClient yönetir; SDK denemesi kapalı, 30 sn zaman aşımı.
        _client = new Client(apiKey: options.ApiKey, httpOptions: new HttpOptions
        {
            Timeout = 30_000,
            RetryOptions = new HttpRetryOptions { Attempts = 1 },
        });
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
