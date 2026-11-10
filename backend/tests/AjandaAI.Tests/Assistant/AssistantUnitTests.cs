// Asistanın saf kuralları (ADR 0025): etkin araç listesi (ASSIST-14), sağlayıcı hatası sınıflaması (ASSIST-25),
// sohbet isteği sınırları (ASSIST-20, 21).

using System.Net;
using AjandaAI.Application.Assistant;
using AjandaAI.Application.Assistant.Dtos;
using AjandaAI.Application.Assistant.Validators;

namespace AjandaAI.Tests.Assistant;

public class AssistantUnitTests
{
    // ASSIST-14
    [Fact]
    public void ASSIST_14_DisabledTool_IsNotEnabled_OthersAre()
    {
        var options = new AssistantToolOptions { DisabledTools = ["HAVA_DURUMU"] };

        Assert.False(options.IsEnabled(AssistantToolNames.HavaDurumu));
        Assert.Equal(AssistantToolNames.All.Count - 1, options.EnabledTools.Count());
        Assert.All(AssistantToolNames.All.Except([AssistantToolNames.HavaDurumu]), n => Assert.True(options.IsEnabled(n)));
    }

    // ASSIST-25: 429 ve 5xx "yoğun"dur; 400/401 değildir. İç istisna da taranır.
    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests, true)]
    [InlineData(HttpStatusCode.InternalServerError, true)]
    [InlineData(HttpStatusCode.ServiceUnavailable, true)]
    [InlineData(HttpStatusCode.BadRequest, false)]
    [InlineData(HttpStatusCode.Unauthorized, false)]
    public void ASSIST_25_ProviderErrors_ClassifiesStatusCodes(HttpStatusCode status, bool busy)
    {
        Assert.Equal(busy, ProviderErrors.IsBusy(new HttpRequestException("x", null, status)));
        Assert.Equal(busy, ProviderErrors.IsBusy(new InvalidOperationException("sarmal", new HttpRequestException("x", null, status))));
    }

    private sealed class ServerError : Exception;

    private sealed class ClientError(int code) : Exception
    {
        public int StatusCode { get; } = code;
    }

    // ASSIST-25: sağlayıcı istisna tipleri (Google.GenAI ServerError / ClientError(429)) Application'da tip görmeden tanınır.
    [Fact]
    public void ASSIST_25_ProviderErrors_RecognizesProviderExceptionShapes()
    {
        Assert.True(ProviderErrors.IsBusy(new ServerError()));
        Assert.True(ProviderErrors.IsBusy(new ClientError(429)));
        Assert.False(ProviderErrors.IsBusy(new ClientError(400)));
        Assert.False(ProviderErrors.IsBusy(new InvalidOperationException()));
    }

    // ASSIST-20, ASSIST-21
    [Fact]
    public void ASSIST_20_21_Validator_EnforcesMessageAndHistoryLimits()
    {
        var validator = new AssistantChatRequestDtoValidator();
        var history20 = Enumerable.Range(0, 20).Select(_ => new AssistantChatMessageDto("user", "x")).ToList();

        Assert.True(validator.Validate(new AssistantChatRequestDto(new string('a', 2000), history20)).IsValid);
        Assert.False(validator.Validate(new AssistantChatRequestDto(new string('a', 2001), null)).IsValid);
        Assert.False(validator.Validate(new AssistantChatRequestDto("a", [.. history20, new("user", "x")])).IsValid);
        Assert.False(validator.Validate(new AssistantChatRequestDto("a", [new("system", "x")])).IsValid);
    }
}
