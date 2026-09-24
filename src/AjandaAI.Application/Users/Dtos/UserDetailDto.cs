// Tek bir User için dönen detay okuma modelidir.
// Navigation property (Activities) içermez.

namespace AjandaAI.Application.Users.Dtos;

public record UserDetailDto(
    int Id,
    string Email,
    string DisplayName,
    string TimeZoneId,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
