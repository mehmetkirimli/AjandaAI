// IEmailSender'ın geliştirme implementasyonu: e-postayı göndermez, içeriğini (doğrulama linki dahil) log'a yazar.
// YALNIZCA Development'ta kayıtlıdır (AddDevelopmentInfrastructure, ADR 0018 Karar 8): log'a gizli token yazar,
// Production'da OLMAMALI. Link web istemcisinin sayfasını gösterir (Frontend:BaseUrl, ADR 0021);
// geliştirici linke tıklayarak doğrulamayı tamamlar. Alıcı e-posta maskelenir.

using AjandaAI.Application.Auth;
using AjandaAI.Application.Common;
using AjandaAI.Application.Common.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AjandaAI.Infrastructure.Auth;

public sealed class LogEmailSender : IEmailSender
{
    private readonly ILogger<LogEmailSender> _logger;
    private readonly FrontendOptions _frontend;

    public LogEmailSender(ILogger<LogEmailSender> logger, IOptions<FrontendOptions> frontend)
    {
        _logger = logger;
        _frontend = frontend.Value;
    }

    public Task SendEmailVerificationAsync(string toEmail, string displayName, string verificationToken, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[DEV E-POSTA] Doğrulama {Email}: {VerifyUrl}",
            MaskingHelper.MaskEmail(toEmail), _frontend.VerifyEmailUrl(verificationToken));
        return Task.CompletedTask;
    }

    public Task SendAccountAlreadyExistsAsync(string toEmail, string displayName, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[DEV E-POSTA] Zaten hesabınız var bildirimi {Email}", MaskingHelper.MaskEmail(toEmail));
        return Task.CompletedTask;
    }
}
