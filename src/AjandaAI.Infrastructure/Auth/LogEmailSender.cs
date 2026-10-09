// IEmailSender'ın geliştirme implementasyonu: e-postayı göndermez, içeriğini (doğrulama token'ı dahil) log'a yazar.
// YALNIZCA Development'ta kayıtlıdır (Program.cs, ADR 0018 Karar 8): log'a gizli token yazar, Production'da OLMAMALI.
// Alıcı e-posta maskelenir; token geliştiricinin doğrulamayı tamamlayabilmesi için bilerek açık yazılır.

using AjandaAI.Application.Auth;
using AjandaAI.Application.Common.Logging;
using Microsoft.Extensions.Logging;

namespace AjandaAI.Infrastructure.Auth;

public sealed class LogEmailSender : IEmailSender
{
    private readonly ILogger<LogEmailSender> _logger;

    public LogEmailSender(ILogger<LogEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendEmailVerificationAsync(string toEmail, string displayName, string verificationToken, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[DEV E-POSTA] Doğrulama {Email}: POST /api/auth/verify-email gövde {{\"token\":\"{VerificationToken}\"}}",
            MaskingHelper.MaskEmail(toEmail), verificationToken);
        return Task.CompletedTask;
    }

    public Task SendAccountAlreadyExistsAsync(string toEmail, string displayName, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[DEV E-POSTA] Zaten hesabınız var bildirimi {Email}", MaskingHelper.MaskEmail(toEmail));
        return Task.CompletedTask;
    }
}
