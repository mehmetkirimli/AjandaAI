// Category kaynağının API'ye dönen okuma modelidir.
// Ham entity yerine bu DTO döner; navigation property içermez.

namespace AjandaAI.Application.Categories.Dtos;

public record CategoryListDto(int Id, string Name, bool IsActive);
