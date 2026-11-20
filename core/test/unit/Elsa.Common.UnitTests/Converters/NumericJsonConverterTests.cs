using System.Numerics;
using System.Text.Json;
using Elsa.Common.Converters;

namespace Elsa.Common.UnitTests.Converters;

/// <summary>
/// Invalid or out-of-range input must surface as <see cref="JsonException"/>, which is what System.Text.Json callers catch.
/// </summary>
public class NumericJsonConverterTests
{
    [Theory]
    [InlineData("\"42\"", 42)]
    [InlineData("42", 42)]
    public void IntegerJsonConverter_ReadsValidInput(string json, int expected) =>
        Assert.Equal(expected, Deserialize<int>(json, new IntegerJsonConverter()));

    [Theory]
    [InlineData("\"abc\"")]          // invalid string
    [InlineData("\"99999999999\"")]  // out-of-range string
    [InlineData("99999999999")]      // out-of-range number
    public void IntegerJsonConverter_ThrowsJsonExceptionForInvalidInput(string json) =>
        Assert.Throws<JsonException>(() => Deserialize<int>(json, new IntegerJsonConverter()));

    [Theory]
    [InlineData("\"1.5\"")]
    [InlineData("1.5")]
    public void DecimalJsonConverter_ReadsValidInput(string json) =>
        Assert.Equal(1.5m, Deserialize<decimal>(json, new DecimalJsonConverter()));

    [Theory]
    [InlineData("\"abc\"")]      // invalid string
    [InlineData("\"79228162514264337593543950336\"")] // out-of-range string (decimal.MaxValue + 1)
    [InlineData("1e30")]         // out-of-range number
    public void DecimalJsonConverter_ThrowsJsonExceptionForInvalidInput(string json) =>
        Assert.Throws<JsonException>(() => Deserialize<decimal>(json, new DecimalJsonConverter()));

    [Theory]
    [InlineData("\"42\"")]
    [InlineData("42")]
    public void BigIntegerJsonConverter_ReadsValidInput(string json) =>
        Assert.Equal(new BigInteger(42), Deserialize<BigInteger>(json, new BigIntegerJsonConverter()));

    [Theory]
    [InlineData("\"abc\"")]                     // invalid string
    [InlineData("\"99999999999999999999\"")]    // out-of-range string
    [InlineData("99999999999999999999")]        // out-of-range number
    public void BigIntegerJsonConverter_ThrowsJsonExceptionForInvalidInput(string json) =>
        Assert.Throws<JsonException>(() => Deserialize<BigInteger>(json, new BigIntegerJsonConverter()));

    private static T? Deserialize<T>(string json, System.Text.Json.Serialization.JsonConverter converter) =>
        JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { Converters = { converter } });
}
