// Mevcut kullanıcıyı güncelleme isteğinin girdi modelidir.
// Id route'tan gelir.

namespace AjandaAI.Application.Users.Dtos;

public record UserUpdateDto(string Email, string DisplayName, string TimeZoneId);
