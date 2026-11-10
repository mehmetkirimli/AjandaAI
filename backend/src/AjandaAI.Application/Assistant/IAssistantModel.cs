// Dil modeline erişim (ADR 0025 §2). Application sağlayıcı tipini görmez; yalnızca IChatClient soyutlamasını.
using Microsoft.Extensions.AI;

namespace AjandaAI.Application.Assistant;

public interface IAssistantModel
{
    /// <summary>Anahtar tanımlı değilse null.</summary>
    IChatClient? Client { get; }

    bool IsConfigured => Client is not null;
}
