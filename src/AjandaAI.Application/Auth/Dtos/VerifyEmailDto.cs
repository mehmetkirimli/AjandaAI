// E-posta doğrulama isteği: doğrulama bağlantısındaki düz metin token.

namespace AjandaAI.Application.Auth.Dtos;

public record VerifyEmailDto(string Token);
