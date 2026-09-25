// Controller'dan dönen ApiResponse<T>'nin ResultType'ına göre HTTP status kodunu belirler.
// Body olduğu gibi kalır; status kodu body'ye yazılmaz.
// Controller'lar ActionResult kullanmaz, status kodu tek yerden (burada) hesaplanır.

using AjandaAI.Application.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AjandaAI.Api.Filters;

public class ApiResponseFilter : IAsyncResultFilter
{
    public Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.Result is ObjectResult { Value: IApiResponse response } objectResult)
        {
            if (response.ResultType == ResultType.NoContent)
            {
                // 204 yanıtı gövde taşıyamaz (HTTP kuralı); gövdesiz sonuçla değiştirilir.
                context.Result = new NoContentResult();
            }
            else
            {
                objectResult.StatusCode = ToStatusCode(response.ResultType);
                if (response.ResultType == ResultType.Created)
                {
                    SetLocationHeader(context.HttpContext, response);
                }
            }
        }

        return next();
    }

    // Location = istek yolu + "/" + Data.Id. Data'da Id yoksa header atlanır.
    private static void SetLocationHeader(HttpContext httpContext, IApiResponse response)
    {
        var data = response.GetType().GetProperty("Data")?.GetValue(response);
        var id = data?.GetType().GetProperty("Id")?.GetValue(data);
        if (id is null)
        {
            return;
        }

        var path = httpContext.Request.Path.Value?.TrimEnd('/');
        httpContext.Response.Headers.Location = $"{path}/{id}";
    }

    private static int ToStatusCode(ResultType resultType) => resultType switch
    {
        ResultType.Success => StatusCodes.Status200OK,
        ResultType.Created => StatusCodes.Status201Created,
        ResultType.NoContent => StatusCodes.Status204NoContent,
        ResultType.ValidationError => StatusCodes.Status400BadRequest,
        ResultType.NotFound => StatusCodes.Status404NotFound,
        ResultType.Conflict => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status500InternalServerError
    };
}
