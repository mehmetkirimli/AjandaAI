// POST /api/assistant/chat istek/yanıt modelleri (ADR 0025 §4). Konuşma sunucuda saklanmaz; geçmişi istemci yollar.
// Öneri (proposal) POST /api/activities (Activity) veya /api/activities/series (Series) gövdesine birebir uyar;
// panel "Deftere yaz"da bu gövdeyi olduğu gibi gönderir (ASSIST-42).

using AjandaAI.Application.Activities.Dtos;

namespace AjandaAI.Application.Assistant.Dtos;

public record AssistantChatMessageDto(string Role, string Text);

public record AssistantChatRequestDto(string Message, List<AssistantChatMessageDto>? History);

public record AssistantProposalDto(string Kind, ActivityCreateDto? Activity, ActivitySeriesCreateDto? Series)
{
    public const string ActivityKind = "activity";
    public const string SeriesKind = "series";
}

public record AssistantChatResponseDto(string Reply, IReadOnlyList<AssistantProposalDto> Proposals, IReadOnlyList<string> UsedTools);
