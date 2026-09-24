// Yeni kullanıcı oluşturma isteğinin girdi modelidir.

namespace AjandaAI.Application.Users.Dtos;

public record UserCreateDto(string Email, string DisplayName, string TimeZoneId);
