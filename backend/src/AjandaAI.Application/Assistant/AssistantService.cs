// Uygulama içi asistanın sohbet servisi (ADR 0025 §4). Sistem talimatı Türkçe; kullanıcının saat dilimi ve yerel
// "bugün"ü içerir (ASSIST-31). Araç döngüsü FunctionInvokingChatClient ile; tek istekte en fazla 8 araç çağrısı (ASSIST-24).
// Anahtar yok → 503 "yapılandırılmadı" (ASSIST-01); sağlayıcı 429/5xx → 503 "yoğun" (ASSIST-25).
// Mesaj ve cevap metni LOGLANMAZ; yalnızca araç adı, süre ve token sayısı loglanır (ASSIST-30, ADR 0016).

using System.Diagnostics;
using System.Globalization;
using AjandaAI.Application.Assistant.Dtos;
using AjandaAI.Application.Common;
using AjandaAI.Application.Users;
using FluentValidation;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace AjandaAI.Application.Assistant;

public class AssistantService
{
    public const int MaxToolCallsPerRequest = 8;
    public const string NotConfiguredMessage = "Asistan yapılandırılmadı.";
    public const string BusyMessage = "Asistan şu an yoğun, biraz sonra dene.";
    public const string ToolLimitMessage =
        "Bu isteği tamamlamak için çok fazla adım gerekti (en fazla 8 araç çağrısı); isteğini daha küçük parçalara bölebilir misin?";

    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");

    private readonly IAssistantModel _model;
    private readonly AssistantToolRegistry _tools;
    private readonly IValidator<AssistantChatRequestDto> _validator;
    private readonly IUserRepository _users;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _time;
    private readonly ILogger<AssistantService> _logger;

    public AssistantService(IAssistantModel model, AssistantToolRegistry tools, IValidator<AssistantChatRequestDto> validator,
        IUserRepository users, ICurrentUser currentUser, TimeProvider time, ILogger<AssistantService> logger)
    {
        _model = model;
        _tools = tools;
        _validator = validator;
        _users = users;
        _currentUser = currentUser;
        _time = time;
        _logger = logger;
    }

    public async Task<ApiResponse<AssistantChatResponseDto>> ChatAsync(AssistantChatRequestDto request, CancellationToken cancellationToken = default)
    {
        if (_model.Client is not { } client)
            return ApiResponse<AssistantChatResponseDto>.ServiceUnavailable(NotConfiguredMessage);

        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return ApiResponse<AssistantChatResponseDto>.Fail("Doğrulama hatası.",
                validation.Errors.Select(e => e.ErrorMessage).ToList());

        var userId = _currentUser.UserId;
        var turn = new AssistantTurn();
        var messages = new List<ChatMessage> { new(ChatRole.System, await BuildSystemPromptAsync(cancellationToken)) };
        messages.AddRange((request.History ?? new()).Select(m =>
            new ChatMessage(m.Role == "assistant" ? ChatRole.Assistant : ChatRole.User, m.Text)));
        messages.Add(new ChatMessage(ChatRole.User, request.Message));

        // Logger verilmez: kütüphanenin ayrıntılı logları araç girdisini (kullanıcı içeriği) yazabilir.
        using var invoking = new FunctionInvokingChatClient(client)
        {
            MaximumIterationsPerRequest = MaxToolCallsPerRequest + 1,
            FunctionInvoker = (context, ct) => InvokeToolAsync(turn, userId, context, ct)
        };

        var stopwatch = Stopwatch.StartNew();
        ChatResponse response;
        try
        {
            response = await invoking.GetResponseAsync(messages, new ChatOptions { Tools = _tools.Build(turn) }, cancellationToken);
        }
        catch (Exception e) when (!cancellationToken.IsCancellationRequested && ProviderErrors.IsBusy(e))
        {
            _logger.LogWarning("Asistan sağlayıcısı yoğun {UserId} {ErrorType} {ElapsedMs}",
                userId, e.GetType().Name, stopwatch.ElapsedMilliseconds);
            return ApiResponse<AssistantChatResponseDto>.ServiceUnavailable(BusyMessage);
        }

        _logger.LogInformation(
            "Asistan yanıtı {UserId} {ElapsedMs} {ToolCalls} {InputTokens} {OutputTokens} {TotalTokens} {Proposals}",
            userId, stopwatch.ElapsedMilliseconds, turn.ToolCallCount, response.Usage?.InputTokenCount,
            response.Usage?.OutputTokenCount, response.Usage?.TotalTokenCount, turn.Proposals.Count);

        var reply = turn.ToolLimitReached ? ToolLimitMessage : response.Text;
        return ApiResponse<AssistantChatResponseDto>.Ok(
            new AssistantChatResponseDto(reply, turn.Proposals, turn.UsedTools.Distinct().ToList()));
    }

    private async ValueTask<object?> InvokeToolAsync(AssistantTurn turn, int userId, FunctionInvocationContext context, CancellationToken ct)
    {
        if (turn.ToolCallCount >= MaxToolCallsPerRequest)
        {
            turn.ToolLimitReached = true;
            context.Terminate = true;
            _logger.LogWarning("Asistan araç sınırı aşıldı {UserId} {Tool}", userId, context.Function.Name);
            return new AssistantToolError("Araç çağrı sınırı aşıldı.");
        }

        turn.ToolCallCount++;
        turn.UsedTools.Add(context.Function.Name);
        var stopwatch = Stopwatch.StartNew();
        object? result;
        try
        {
            result = await context.Function.InvokeAsync(context.Arguments, ct);
        }
        catch (Exception e) when (!ct.IsCancellationRequested && e is ArgumentException or System.Text.Json.JsonException or FormatException)
        {
            // Modelin girdisi bağlanamadı (eksik/yanlış tipte alan): sohbet çökmez, model düzeltebilir.
            result = new AssistantToolError("Araç girdisi geçersiz; alanları ve biçimlerini (ISO 8601 tarih vb.) kontrol et.");
        }
        _logger.LogInformation("Asistan aracı çalıştı {UserId} {Tool} {ElapsedMs} {ToolFailed}",
            userId, context.Function.Name, stopwatch.ElapsedMilliseconds, result is AssistantToolError);
        return result;
    }

    /// <summary>Kullanıcının saat dilimi ve yerel "bugün"ü içeren Türkçe sistem talimatı (ASSIST-31).</summary>
    public async Task<string> BuildSystemPromptAsync(CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(_currentUser.UserId, cancellationToken);
        var zone = user is not null && TimeZoneInfo.TryFindSystemTimeZoneById(user.TimeZoneId, out var z) ? z : TimeZoneInfo.Utc;
        var now = TimeZoneInfo.ConvertTime(_time.GetUtcNow(), zone);

        return string.Join("\n",
            "Sen AjandaAI'nin ajanda asistanısın. Her zaman Türkçe, kısa ve net yanıt ver.",
            $"Kullanıcının saat dilimi: {zone.Id} (UTC{now:zzz}).",
            $"Bugün (kullanıcının yerel tarihi): {now.ToString("yyyy-MM-dd dddd", Turkish)}, saat {now:HH:mm}.",
            "Yalnızca sana verilen araçları kullan; araçların sağlamadığı bilgiyi uydurma.",
            "Tarih, saat, kategori gibi bir bilgiden emin değilsen tahmin etme, kullanıcıya sor.",
            "Tarih ve saatleri kullanıcının saat diliminde, offset'li ISO 8601 biçiminde ver.",
            "Kayıt eklemeden önce ne ekleyeceğini özetle ve aktivite_oner ya da seri_oner aracını kullan. " +
            "Bu araçlar kaydı yazmaz; kullanıcı öneri kartında onaylayınca kayıt oluşur. Kaydın eklendiğini söyleme.",
            "Bir araç hata dönerse hatayı kullanıcıya anlaşılır biçimde açıkla.");
    }
}
