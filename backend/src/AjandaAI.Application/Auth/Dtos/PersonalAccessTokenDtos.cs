// Kişisel erişim anahtarı (PAT) DTO'ları (ADR 0024 §1).
// Düz anahtar YALNIZCA oluşturma yanıtında (PersonalAccessTokenCreatedDto.Token) döner; özet hiçbir DTO'da yoktur.

namespace AjandaAI.Application.Auth.Dtos;

public record PersonalAccessTokenCreateDto(string Name);

public record PersonalAccessTokenDto(
    int Id,
    string Name,
    string Prefix,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? LastUsedAt,
    DateTimeOffset? RevokedAt);

public record PersonalAccessTokenCreatedDto(
    int Id,
    string Name,
    string Prefix,
    string Token,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt);
