// SensitiveDataDestructuringPolicy'nin birim testleridir: hassas alanlar maskelenir,
// diğer alanlar değişmez, null değerler exception üretmez.
// Policy Api katmanında olduğu için bu testler Api'yi referans alan bu projede durur
// (AjandaAI.Tests yalnızca Application ve Domain'i referans alabilir).

using AjandaAI.Api.Logging;
using Serilog.Core;
using Serilog.Events;

namespace AjandaAI.IntegrationTests.Logging;

public class SensitiveDataDestructuringPolicyTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 9, 26, 10, 0, 0, TimeSpan.Zero);

    private record SensitiveDto(
        int Id,
        string? Email,
        string? Phone,
        string? PhoneNumber,
        string? Address,
        string? DisplayName,
        DateTimeOffset CreatedAt);

    private record PlainDto(int Id, string Title);

    private sealed class ScalarFactory : ILogEventPropertyValueFactory
    {
        public LogEventPropertyValue CreatePropertyValue(object? value, bool destructureObjects = false) =>
            new ScalarValue(value);
    }

    private static Dictionary<string, object?> Destructure(object value)
    {
        var policy = new SensitiveDataDestructuringPolicy();
        Assert.True(policy.TryDestructure(value, new ScalarFactory(), out var result));
        var structure = Assert.IsType<StructureValue>(result);
        return structure.Properties.ToDictionary(p => p.Name, p => ((ScalarValue)p.Value).Value);
    }

    [Fact]
    public void SensitiveFields_AreMasked()
    {
        var props = Destructure(new SensitiveDto(
            7, "perihan945@hotmail.com", "05321234567", "+90 532 123 45 67",
            "Atatürk Cad. No:5 Kadıköy", "Perihan Yılmaz", CreatedAt));

        Assert.Equal("per*****45@hot****.com", props["Email"]);
        Assert.Equal("*******4567", props["Phone"]);
        Assert.Equal("*************5 67", props["PhoneNumber"]);
        Assert.Equal("Atatürk***", props["Address"]);
        Assert.Equal("P*************", props["DisplayName"]);
    }

    [Fact]
    public void NonSensitiveFields_AreUnchanged()
    {
        var props = Destructure(new SensitiveDto(
            7, "perihan945@hotmail.com", null, null, null, null, CreatedAt));

        Assert.Equal(7, props["Id"]);
        Assert.Equal(CreatedAt, props["CreatedAt"]);
    }

    [Fact]
    public void NullFields_DoNotThrow_AndBecomeEmpty()
    {
        var props = Destructure(new SensitiveDto(1, null, null, null, null, null, CreatedAt));

        Assert.Equal(string.Empty, props["Email"]);
        Assert.Equal(string.Empty, props["Phone"]);
        Assert.Equal(string.Empty, props["PhoneNumber"]);
        Assert.Equal(string.Empty, props["Address"]);
        Assert.Equal(string.Empty, props["DisplayName"]);
    }

    [Fact]
    public void TypeWithoutSensitiveFields_IsLeftToDefaultDestructuring()
    {
        var policy = new SensitiveDataDestructuringPolicy();

        Assert.False(policy.TryDestructure(new PlainDto(1, "Başlık"), new ScalarFactory(), out _));
    }
}
