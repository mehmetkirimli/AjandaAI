// GET /api/categories için sayfalama parametreleri ([FromQuery]).
// IncludeInactive yalnızca Admin rolünde dikkate alınır; User rolünde servis yok sayar (ADMIN-11, ADR 0023).

using AjandaAI.Application.Common;

namespace AjandaAI.Application.Categories.Dtos;

public class CategoryFilterDto : PageRequest
{
    public bool IncludeInactive { get; set; }
}
