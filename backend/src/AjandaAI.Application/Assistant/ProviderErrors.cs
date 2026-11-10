// Sağlayıcı (Gemini vb.) hatalarının "yoğun" sayılıp sayılmayacağını belirler (ASSIST-25).
// Application sağlayıcı tipini görmez: HttpRequestException durum kodu, tip adı (ServerError) ve
// durum kodu taşıyan özellikler (StatusCode/Code/Status) iç istisnalarla birlikte taranır.

using System.Net;

namespace AjandaAI.Application.Assistant;

public static class ProviderErrors
{
    public static bool IsBusy(Exception exception)
    {
        for (var e = exception; e is not null; e = e.InnerException)
        {
            if (e is HttpRequestException { StatusCode: { } code } && IsBusyCode((int)code))
                return true;
            if (e.GetType().Name == "ServerError")
                return true;

            foreach (var name in new[] { "StatusCode", "Code", "Status" })
            {
                var value = e.GetType().GetProperty(name)?.GetValue(e);
                var number = value switch
                {
                    int i => i,
                    HttpStatusCode s => (int)s,
                    _ => (int?)null
                };
                if (number is { } n && IsBusyCode(n))
                    return true;
            }
        }

        return false;
    }

    private static bool IsBusyCode(int code) => code == 429 || code is >= 500 and < 600;
}
