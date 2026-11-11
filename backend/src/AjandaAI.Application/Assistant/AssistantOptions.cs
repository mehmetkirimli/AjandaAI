// Asistan ayarları (ADR 0025 §1). Anahtar repoya girmez: Development'ta user-secrets, Production'da ortam değişkeni.
namespace AjandaAI.Application.Assistant;

public sealed class AssistantOptions
{
    public const string SectionName = "Assistant";

    /// <summary>Gemini API anahtarı. Boşsa asistan "yapılandırılmadı" der (ASSIST-01).</summary>
    public string? ApiKey { get; set; }

    /// <summary>Model adı; ücretsiz katmandaki bir flash modeli.</summary>
    public string Model { get; set; } = "gemini-3.5-flash";

    /// <summary>Ana model yoğunken (503) denenecek yedek model; boşsa yedek yok.</summary>
    public string? FallbackModel { get; set; } = "gemini-3.5-flash-lite";
}
