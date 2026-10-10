// FrontendOptions: e-postadaki doğrulama linki web istemcisinin sayfasını gösterir (ADR 0021).

using AjandaAI.Application.Common;

namespace AjandaAI.Tests.Common;

public class FrontendOptionsTests
{
    [Theory]
    [InlineData("http://localhost:5173")]
    [InlineData("http://localhost:5173/")]
    public void VerifyEmailUrl_PointsToFrontendPage(string baseUrl)
    {
        var options = new FrontendOptions { BaseUrl = baseUrl };

        Assert.Equal("http://localhost:5173/verify-email?token=abc-DEF_123", options.VerifyEmailUrl("abc-DEF_123"));
    }

    [Fact]
    public void VerifyEmailUrl_EscapesToken()
    {
        var options = new FrontendOptions { BaseUrl = "http://localhost:5173" };

        Assert.Equal("http://localhost:5173/verify-email?token=a%2Bb%3D", options.VerifyEmailUrl("a+b="));
    }
}
