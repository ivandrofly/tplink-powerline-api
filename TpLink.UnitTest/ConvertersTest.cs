using System.Text.Json;
using System.Text.Json.Serialization;
using TpLink.Api.Converters;
using TpLink.Api.Models;
using Xunit;

namespace TpLink.UnitTest;

public class ConvertersTest
{
    private static JsonSerializerOptions With(JsonConverter converter)
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(converter);
        return options;
    }

    [Theory]
    [InlineData("\"on\"", true)]
    [InlineData("\"ON\"", true)]
    [InlineData("\"off\"", false)]
    [InlineData("\"1\"", true)]
    [InlineData("\"0\"", false)]
    [InlineData("\"garbage\"", false)]
    [InlineData("true", true)]
    [InlineData("false", false)]
    [InlineData("1", true)]
    [InlineData("0", false)]
    [InlineData("null", false)]
    public void StringBoolReadsEveryTokenKind(string json, bool expected)
    {
        Assert.Equal(expected, JsonSerializer.Deserialize<bool>(json, With(new StringBoolConverter())));
    }

    [Fact]
    public void StringBoolWritesOnOff()
    {
        var options = With(new StringBoolConverter());
        Assert.Equal("\"on\"", JsonSerializer.Serialize(true, options));
        Assert.Equal("\"off\"", JsonSerializer.Serialize(false, options));
    }

    [Fact]
    public void StringBoolRejectsStructuralTokens()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<bool>("[]", With(new StringBoolConverter())));
    }

    [Theory]
    [InlineData("\"1\"", true)]
    [InlineData("\"0\"", false)]
    [InlineData("\"on\"", true)]
    [InlineData("true", true)]
    [InlineData("1", true)]
    [InlineData("0", false)]
    [InlineData("null", false)]
    public void BoolToBitReadsEveryTokenKind(string json, bool expected)
    {
        Assert.Equal(expected, JsonSerializer.Deserialize<bool>(json, With(new BoolToBitConvert())));
    }

    [Fact]
    public void BoolToBitWritesBits()
    {
        var options = With(new BoolToBitConvert());
        Assert.Equal("\"1\"", JsonSerializer.Serialize(true, options));
        Assert.Equal("\"0\"", JsonSerializer.Serialize(false, options));
    }

    [Theory]
    [InlineData("\"20\"", 20)]
    [InlineData("\"-3\"", -3)]
    [InlineData("20", 20)]
    [InlineData("true", 1)]
    [InlineData("null", 0)]
    public void IntToStringReadsEveryTokenKind(string json, int expected)
    {
        Assert.Equal(expected, JsonSerializer.Deserialize<int>(json, With(new IntToString())));
    }

    [Theory]
    [InlineData("\"abc\"")]
    [InlineData("\"\"")]
    [InlineData("{}")]
    public void IntToStringRejectsNonNumeric(string json)
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<int>(json, With(new IntToString())));
    }

    [Fact]
    public void IntToStringWritesString()
    {
        Assert.Equal("\"20\"", JsonSerializer.Serialize(20, With(new IntToString())));
    }

    [Theory]
    [InlineData("\"127\"", (byte)127)]
    [InlineData("\"6\"", (byte)6)]
    [InlineData("127", (byte)127)]
    [InlineData("\"Monday, Tuesday\"", (byte)6)]
    [InlineData("\"sunday\"", (byte)1)]
    [InlineData("null", (byte)0)]
    public void DaysReadsEveryTokenKind(string json, byte expected)
    {
        Assert.Equal((Days)expected, JsonSerializer.Deserialize<Days>(json, With(new DaysEnumToCustomString())));
    }

    [Theory]
    [InlineData("\"bogus\"")]
    [InlineData("[]")]
    public void DaysRejectsUnknownValues(string json)
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Days>(json, With(new DaysEnumToCustomString())));
    }

    [Fact]
    public void DaysWritesBitmaskString()
    {
        Assert.Equal("\"6\"", JsonSerializer.Serialize(Days.Monday | Days.Tuesday, With(new DaysEnumToCustomString())));
    }
}
