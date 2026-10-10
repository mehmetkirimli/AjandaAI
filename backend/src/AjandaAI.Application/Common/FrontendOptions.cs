// "Frontend" konfigürasyon bölümü (ADR 0021). E-postadaki bağlantılar API'yi değil web
// istemcisinin sayfalarını gösterir: {BaseUrl}/verify-email?token=...
// Development'ta Vite sunucusudur (http://localhost:5173); Production değeri canlıya çıkışta verilir.

namespace AjandaAI.Application.Common;

public class FrontendOptions
{
    public const string SectionName = "Frontend";

    public string BaseUrl { get; set; } = string.Empty;

    public string VerifyEmailUrl(string token) =>
        $"{BaseUrl.TrimEnd('/')}/verify-email?token={Uri.EscapeDataString(token)}";
}
