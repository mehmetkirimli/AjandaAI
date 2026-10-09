// Refresh ve logout isteklerinin ortak gövdesi: düz metin refresh token (ADR 0018: taşıma request body).

namespace AjandaAI.Application.Auth.Dtos;

public record RefreshTokenRequestDto(string RefreshToken);
