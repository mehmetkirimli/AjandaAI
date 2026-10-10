// GET /api/auth/me yanıtı: oturumdaki kullanıcı (ADR 0021). Web istemcisi üst menüde adı ve
// admin menüsünün görünürlüğü için rolü buradan okur. Şifre hash'i ve token bilgisi içermez.

using AjandaAI.Domain.Enums;

namespace AjandaAI.Application.Auth.Dtos;

public record MeDto(int Id, string Email, string DisplayName, string TimeZoneId, UserRole Role);
