// Kullanıcının kendi profilini düzenlemesi (ADR 0021, profil sayfası).
// E-posta değişikliği BİLEREK yok: yeni adresin doğrulanması gerekir, bildirim aşamasına kalır.

namespace AjandaAI.Application.Auth.Dtos;

// Location isteğe bağlıdır; boş/null gönderilirse temizlenir.
public record ProfileUpdateDto(string DisplayName, string TimeZoneId, string? Location = null);

/// <summary>Şifreler Trim() edilmez (ADR 0018 Karar 10).</summary>
public record ChangePasswordDto(string CurrentPassword, string NewPassword);
