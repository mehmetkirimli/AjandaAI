// GET /api/categories için sayfalama parametreleridir ([FromQuery]); filtre yoktur.

using AjandaAI.Application.Common;

namespace AjandaAI.Application.Categories.Dtos;

public class CategoryFilterDto : PageRequest
{
}
