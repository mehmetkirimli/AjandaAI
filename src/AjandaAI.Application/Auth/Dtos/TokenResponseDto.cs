// Login ve refresh yanıtı: access token (JWT) ile düz metin refresh token ve son kullanma zamanları.

namespace AjandaAI.Application.Auth.Dtos;

public record TokenResponseDto(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);
