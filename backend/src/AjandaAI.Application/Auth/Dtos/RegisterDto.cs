// Kayıt isteği. Rol alanı YOKTUR: gövdede "role" gelse de yok sayılır (ADR 0018 Karar 5).

namespace AjandaAI.Application.Auth.Dtos;

public record RegisterDto(string Email, string DisplayName, string TimeZoneId, string Password);
