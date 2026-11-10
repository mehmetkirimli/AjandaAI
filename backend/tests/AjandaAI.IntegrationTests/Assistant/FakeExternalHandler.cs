// Asistanın dış HTTP servislerini (Open-Meteo, Nominatim, Nager.Date) taklit eden handler (ASSIST-15..17).
// Host'a göre yanıt üretir; her isteği (URL, User-Agent, zaman) kaydeder. Responder testte değiştirilebilir.

using System.Collections.Concurrent;
using System.Net;
using System.Text;

namespace AjandaAI.IntegrationTests.Assistant;

public sealed record ExternalRequest(string Host, string Url, string UserAgent, DateTimeOffset At);

public sealed class FakeExternalHandler : HttpMessageHandler
{
    public const string Weather = "api.open-meteo.com";
    public const string Places = "nominatim.openstreetmap.org";
    public const string Holidays = "date.nager.at";

    public ConcurrentQueue<ExternalRequest> Requests { get; } = new();

    public Func<HttpRequestMessage, HttpResponseMessage> Responder { get; set; } = Default;

    public List<ExternalRequest> For(string host) => Requests.Where(r => r.Host == host).ToList();

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Enqueue(new ExternalRequest(request.RequestUri!.Host, request.RequestUri.AbsoluteUri,
            request.Headers.UserAgent.ToString(), DateTimeOffset.UtcNow));
        return Task.FromResult(Responder(request));
    }

    public static HttpResponseMessage Json(string json) =>
        new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    public static HttpResponseMessage Default(HttpRequestMessage request) => request.RequestUri!.Host switch
    {
        Weather => Json(
            "{\"daily\":{\"time\":[\"2030-01-05\"],\"weather_code\":[0],\"temperature_2m_max\":[18.5]," +
            "\"temperature_2m_min\":[9.1],\"precipitation_probability_max\":[5]}}"),
        Places => Json(
            "[{\"display_name\":\"Belgrad Ormani, Sariyer, Istanbul\",\"lat\":\"41.18\",\"lon\":\"28.98\",\"type\":\"forest\"}]"),
        Holidays => Json(
            "[{\"date\":\"2030-04-23\",\"localName\":\"Ulusal Egemenlik ve Cocuk Bayrami\",\"name\":\"Sovereignty Day\"}]"),
        _ => new HttpResponseMessage(HttpStatusCode.NotFound)
    };
}
