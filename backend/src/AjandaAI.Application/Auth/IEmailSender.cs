// E-posta gönderim soyutlaması (ADR 0018 Karar 8). Implementasyon Infrastructure/Auth'tadır.
// LogEmailSender yalnızca Development'ta kayıtlıdır; SmtpEmailSender bildirim adımında gelir.
// Implementasyonlar tekil (singleton) ve iş parçacığı güvenli olmalıdır: gönderim arka planda çağrılır.

namespace AjandaAI.Application.Auth;

public interface IEmailSender
{
    /// <summary>Doğrulama bağlantısındaki düz metin token'ı alıcıya iletir.</summary>
    Task SendEmailVerificationAsync(string toEmail, string displayName, string verificationToken, CancellationToken cancellationToken = default);

    /// <summary>Kayıtlı ve doğrulanmış bir adresle yeni kayıt denendiğinde "zaten hesabınız var" bildirimi.</summary>
    Task SendAccountAlreadyExistsAsync(string toEmail, string displayName, CancellationToken cancellationToken = default);
}
