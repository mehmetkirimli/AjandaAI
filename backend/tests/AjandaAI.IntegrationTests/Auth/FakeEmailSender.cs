// Testlerde LogEmailSender yerine kullanılan sahte IEmailSender: gönderilenleri bellekte tutar.
// Gönderim arka planda yapıldığı için testler WaitForAsync ile belirli bir mailin gelmesini bekler.

using System.Collections.Concurrent;
using AjandaAI.Application.Auth;

namespace AjandaAI.IntegrationTests.Auth;

public enum EmailKind
{
    Verification,
    AccountExists
}

public sealed record SentEmail(EmailKind Kind, string To, string? Token);

public sealed class FakeEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<SentEmail> _sent = new();

    public IReadOnlyCollection<SentEmail> Sent => _sent.ToArray();

    public Task SendEmailVerificationAsync(string toEmail, string displayName, string verificationToken, CancellationToken cancellationToken = default)
    {
        _sent.Enqueue(new SentEmail(EmailKind.Verification, toEmail, verificationToken));
        return Task.CompletedTask;
    }

    public Task SendAccountAlreadyExistsAsync(string toEmail, string displayName, CancellationToken cancellationToken = default)
    {
        _sent.Enqueue(new SentEmail(EmailKind.AccountExists, toEmail, null));
        return Task.CompletedTask;
    }

    public int Count(EmailKind kind, string to) => Matching(kind, to).Count();

    /// <summary>Belirtilen alıcıya giden n'inci (1 tabanlı) mail gelene kadar bekler.</summary>
    public async Task<SentEmail> WaitForAsync(EmailKind kind, string to, int nth = 1)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (DateTime.UtcNow < deadline)
        {
            var match = Matching(kind, to).Skip(nth - 1).FirstOrDefault();
            if (match is not null)
                return match;
            await Task.Delay(20);
        }
        throw new TimeoutException($"{kind} maili gelmedi: {to} (n={nth})");
    }

    private IEnumerable<SentEmail> Matching(EmailKind kind, string to) =>
        _sent.Where(m => m.Kind == kind && string.Equals(m.To, to, StringComparison.OrdinalIgnoreCase));
}
