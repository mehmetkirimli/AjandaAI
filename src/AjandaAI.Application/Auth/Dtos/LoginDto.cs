// Giriş isteği. Şifre Trim() edilmez (ADR 0018 Karar 10).

namespace AjandaAI.Application.Auth.Dtos;

public record LoginDto(string Email, string Password);
