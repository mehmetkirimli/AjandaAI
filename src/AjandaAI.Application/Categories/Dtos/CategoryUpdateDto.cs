// Mevcut kategoriyi güncelleme isteğinin girdi modelidir.
// IsActive ile pasif kategori yeniden aktifleştirilebilir.

namespace AjandaAI.Application.Categories.Dtos;

public record CategoryUpdateDto(string Name, bool IsActive);
