// Tek bir sohbet isteğinin (bir kullanıcı mesajı) araç durumu: üretilen öneriler, kullanılan araçlar, çağrı sayısı.
// İstek başına yeni örnek oluşturulur; araçlar öneriyi buraya ekler, veritabanına yazmaz (ADR 0025 §3).

using AjandaAI.Application.Assistant.Dtos;

namespace AjandaAI.Application.Assistant;

public sealed class AssistantTurn
{
    public List<AssistantProposalDto> Proposals { get; } = new();

    public List<string> UsedTools { get; } = new();

    public int ToolCallCount { get; set; }

    public bool ToolLimitReached { get; set; }
}
