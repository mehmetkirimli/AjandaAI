// Servis sonucunun türüdür; ApiResponseFilter bunu HTTP status koduna çevirir.
// JSON'a yazılmaz (tek kaynak ilkesi: status kodu yalnızca HTTP katmanında).

namespace AjandaAI.Application.Common;

public enum ResultType
{
    Success,
    Created,
    NoContent,
    ValidationError,
    NotFound,
    Conflict,
    Error
}
