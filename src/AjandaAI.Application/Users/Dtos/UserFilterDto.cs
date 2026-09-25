// GET /api/users için sayfalama + opsiyonel arama parametreleridir ([FromQuery]).
// Search, Email veya DisplayName içinde büyük/küçük harf duyarsız arar.

using AjandaAI.Application.Common;

namespace AjandaAI.Application.Users.Dtos;

public class UserFilterDto : PageRequest
{
    public string? Search { get; set; }
}
