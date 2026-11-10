// Etkin araç listesinin TEK kaynağı (ADR 0025 §3, ASSIST-14). İlk sürümde hepsi açık; ayarla
// ("Assistant:DisabledTools") kapatılan araç modele hiç sunulmaz. Rol/profil bazlı aç/kapa sonraki turdur.

namespace AjandaAI.Application.Assistant;

public sealed class AssistantToolOptions
{
    public IReadOnlyCollection<string> DisabledTools { get; init; } = Array.Empty<string>();

    public IEnumerable<string> EnabledTools =>
        AssistantToolNames.All.Where(n => !DisabledTools.Contains(n, StringComparer.OrdinalIgnoreCase));

    public bool IsEnabled(string name) => EnabledTools.Contains(name);
}
