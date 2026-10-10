// MaskingHelper'ın email, telefon, adres ve genel maskeleme davranışının birim testleridir.
// Her metod için normal, kısa, null ve boş girdi senaryoları doğrulanır.

using AjandaAI.Application.Common.Logging;

namespace AjandaAI.Tests.Common;

public class MaskingHelperTests
{
    // --- MaskEmail ---

    [Theory]
    [InlineData("perihan945@hotmail.com", "per*****45@hot****.com")]
    [InlineData("  mehmet.k@gmail.com ", "meh***.k@gma**.com")]
    [InlineData("ali.veli@sirket.com.tr", "ali***li@sir*******.tr")]
    public void MaskEmail_Normal_MasksLocalAndDomain(string input, string expected)
    {
        Assert.Equal(expected, MaskingHelper.MaskEmail(input));
    }

    [Theory]
    [InlineData("ab@cd.com", "**@**.com")]
    [InlineData("abc@xyz.io", "ab*@xy*.io")]
    public void MaskEmail_Short_HidesAtLeastOneCharacter(string input, string expected)
    {
        Assert.Equal(expected, MaskingHelper.MaskEmail(input));
    }

    [Fact]
    public void MaskEmail_Null_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, MaskingHelper.MaskEmail(null));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void MaskEmail_Empty_ReturnsEmpty(string input)
    {
        Assert.Equal(string.Empty, MaskingHelper.MaskEmail(input));
    }

    [Fact]
    public void MaskEmail_WithoutAtSign_MasksAsGeneric()
    {
        Assert.Equal("gec******iz", MaskingHelper.MaskEmail("gecersizmiz"));
    }

    // --- MaskPhone ---

    [Theory]
    [InlineData("05321234567", "*******4567")]
    [InlineData("+90 532 123 45 67", "*************5 67")]
    public void MaskPhone_Normal_KeepsLastFour(string input, string expected)
    {
        Assert.Equal(expected, MaskingHelper.MaskPhone(input));
    }

    [Theory]
    [InlineData("12", "**")]
    [InlineData("1234", "*234")]
    public void MaskPhone_Short_HidesAtLeastOneCharacter(string input, string expected)
    {
        Assert.Equal(expected, MaskingHelper.MaskPhone(input));
    }

    [Fact]
    public void MaskPhone_Null_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, MaskingHelper.MaskPhone(null));
    }

    [Fact]
    public void MaskPhone_Empty_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, MaskingHelper.MaskPhone(""));
    }

    // --- MaskGeneric ---

    [Theory]
    [InlineData("1234567890", 2, 3, "12*****890")]
    [InlineData("Mehmet", 1, 0, "M*****")]
    [InlineData("abcdef", -1, -5, "******")]
    public void MaskGeneric_Normal_KeepsRequestedEnds(string input, int start, int end, string expected)
    {
        Assert.Equal(expected, MaskingHelper.MaskGeneric(input, start, end));
    }

    [Theory]
    [InlineData("ab", 1, 1, "**")]
    [InlineData("abcd", 3, 3, "abc*")]
    public void MaskGeneric_Short_HidesAtLeastOneCharacter(string input, int start, int end, string expected)
    {
        Assert.Equal(expected, MaskingHelper.MaskGeneric(input, start, end));
    }

    [Fact]
    public void MaskGeneric_Null_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, MaskingHelper.MaskGeneric(null, 2, 2));
    }

    [Fact]
    public void MaskGeneric_Empty_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, MaskingHelper.MaskGeneric("", 2, 2));
    }

    // --- MaskAddress ---

    [Fact]
    public void MaskAddress_Normal_KeepsFirstWord()
    {
        Assert.Equal("Atatürk***", MaskingHelper.MaskAddress("Atatürk Cad. No:5 Kadıköy/İstanbul"));
    }

    [Fact]
    public void MaskAddress_Short_KeepsSingleWord()
    {
        Assert.Equal("Ev***", MaskingHelper.MaskAddress("  Ev "));
    }

    [Fact]
    public void MaskAddress_Null_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, MaskingHelper.MaskAddress(null));
    }

    [Fact]
    public void MaskAddress_Empty_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, MaskingHelper.MaskAddress(""));
    }
}
