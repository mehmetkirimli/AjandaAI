// Ücretsiz katmanda Gemini sık sık anlık 503 "yoğun" döner (2026-11-10 kabul denemesi).
// Yoğunlukta: kısa bekleyip bir kez daha dener, sonra yedek modele (Assistant:FallbackModel) geçer.
// Hâlâ yoğunsa hatayı yukarı bırakır → AssistantService 503 "Asistan şu an yoğun" der (ASSIST-25).
using AjandaAI.Application.Assistant;
using Microsoft.Extensions.AI;

namespace AjandaAI.Infrastructure.Assistant;

internal sealed class BusyRetryChatClient : DelegatingChatClient
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(1.5);
    private readonly IChatClient? _fallback;

    public BusyRetryChatClient(IChatClient primary, IChatClient? fallback) : base(primary) => _fallback = fallback;

    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var list = messages as IList<ChatMessage> ?? messages.ToList();
        try
        {
            return await base.GetResponseAsync(list, options, cancellationToken);
        }
        catch (Exception e) when (!cancellationToken.IsCancellationRequested && ProviderErrors.IsBusy(e))
        {
            await Task.Delay(RetryDelay, cancellationToken);
        }

        try
        {
            return await base.GetResponseAsync(list, options, cancellationToken);
        }
        catch (Exception e) when (_fallback is not null && !cancellationToken.IsCancellationRequested && ProviderErrors.IsBusy(e))
        {
            return await _fallback.GetResponseAsync(list, options, cancellationToken);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _fallback?.Dispose();
        base.Dispose(disposing);
    }
}
