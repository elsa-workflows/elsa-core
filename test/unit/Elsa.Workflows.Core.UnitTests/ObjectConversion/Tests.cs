using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.Expressions.Exceptions;
using Elsa.Expressions.Helpers;

namespace Elsa.Workflows.Core.UnitTests.ObjectConversion;

public class Tests
{
    private readonly ObjectConverterOptions _objectConverterOptions = new(StrictMode: true);
    
    [Fact]
    public void TryConvertTo_SameType_ReturnsSuccess()
    {
        // Arrange
        var value = 42;

        // Act
        var result = value.TryConvertTo<int>(_objectConverterOptions);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void TryConvertTo_DifferentType_ReturnsConvertedValue()
    {
        // Arrange
        var value = "42";

        // Act
        var result = value.TryConvertTo<int>(_objectConverterOptions);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void TryConvertTo_InvalidConversion_ReturnsFailure()
    {
        // Arrange
        var value = "invalid";

        // Act
        var result = value.TryConvertTo<int>(_objectConverterOptions);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.Exception);
    }

    [Fact]
    public void TryConvertTo_InvalidJsonString_ReturnsFailure()
    {
        // Arrange
        var value = "{ invalid json }";

        // Act
        var result = value.TryConvertTo<Dictionary<string, object>>(_objectConverterOptions);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.Exception);
    }

    [Fact]
    public void ConvertTo_NullValue_ReturnsDefault()
    {
        // Arrange
        object? value = null;

        // Act
        var result = value.ConvertTo<int>(_objectConverterOptions);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void ConvertTo_JsonElementNumberToString_ReturnsString()
    {
        // Arrange
        var jsonElement = JsonNode.Parse("42")!.AsValue();

        // Act
        var result = jsonElement.ConvertTo<string>();

        // Assert
        Assert.Equal("42", result);
    }

    [Fact]
    public void ConvertTo_JsonNodeToExpandoObject_ReturnsExpandoObject()
    {
        // Arrange
        var jsonNode = JsonNode.Parse("{ \"key\": \"value\" }");

        // Act
        var result = jsonNode.ConvertTo<ExpandoObject>(_objectConverterOptions);

        // Assert
        dynamic expando = Assert.IsType<ExpandoObject>(result);
        object value = expando.key;

        // This is not the result I expect, I would have expected the JsonNode to have been recursively converted

        Assert.True(value is JsonElement);
        Assert.Equal(JsonValueKind.String, ((JsonElement)value).ValueKind);
        Assert.Equal("value", ((JsonElement)value).GetString());
    }

    [Fact]
    public void ConvertTo_StringToDateTime_ReturnsDateTime()
    {
        // Arrange
        var value = "2023-01-01T00:00:00";

        // Act
        var result = value.ConvertTo<DateTime>(_objectConverterOptions);

        // Assert
        Assert.Equal(new(2023, 1, 1, 0, 0, 0), result);
    }

    [Fact]
    public void ConvertTo_StringToEnum_ReturnsEnum()
    {
        // Arrange
        var value = "Monday";

        // Act
        var result = value.ConvertTo<DayOfWeek>(_objectConverterOptions);

        // Assert
        Assert.Equal(DayOfWeek.Monday, result);
    }

    [Fact]
    public void ConvertTo_StringToByteArray_ReturnsByteArray()
    {
        // Arrange
        var value = Convert.ToBase64String(new byte[]
        {
            1, 2, 3
        });

        // Act
        var result = value.ConvertTo<byte[]>(_objectConverterOptions);

        // Assert
        Assert.Equal(new byte[]
        {
            1, 2, 3
        }, result);
    }

    [Fact]
    public void ConvertTo_InvalidJsonString_ThrowsException()
    {
        // Arrange
        var value = "{ invalid json }";

        // Act & Assert
        Assert.Throws<TypeConversionException>(() => value.ConvertTo<Dictionary<string, object>>(_objectConverterOptions));
    }

    [Fact]
    public void ConvertTo_EnumerableToList_ReturnsConvertedList()
    {
        // Arrange
        var value = new[]
        {
            "1", "2", "3"
        };

        // Act
        var result = value.ConvertTo<List<int>>(_objectConverterOptions);

        // Assert
        Assert.Equal([
            1,
            2,
            3
        ], result);
    }

    [Fact]
    public void ConvertTo_DateTimeToDateOnly_ReturnsDateOnly()
    {
        // Arrange
        var value = new DateTime(2023, 1, 1);

        // Act
        var result = value.ConvertTo<DateOnly>(_objectConverterOptions);

        // Assert
        Assert.Equal(new(2023, 1, 1), result);
    }

    [Fact]
    public void ConvertTo_DateOnlyToDateTime_ReturnsDateTime()
    {
        // Arrange
        var value = new DateOnly(2023, 1, 1);

        // Act
        var result = value.ConvertTo<DateTime>(_objectConverterOptions);

        // Assert
        Assert.Equal(new(2023, 1, 1, 0, 0, 0), result);
    }

    [Fact]
    public void ConvertTo_UnknownConversion_ThrowsInvalidCastException()
    {
        // Arrange
        var value = new object();

        // Act & Assert
        Assert.Throws<TypeConversionException>(() => value.ConvertTo<int>(_objectConverterOptions));
    }

    [Fact]
    public void ConvertTo_JsonArrayToListOfObject_ReturnsListOfJsonObject()
    {
        // Arrange
        var jsonArrayString = "[{\"name\":\"Alice\",\"age\":30},{\"name\":\"Bob\",\"age\":25}]";
        var jsonArray = JsonNode.Parse(jsonArrayString);

        // Act
        var result = jsonArray.ConvertTo<List<object>>(_objectConverterOptions);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.IsType<JsonObject>(result[0]);
        Assert.IsType<JsonObject>(result[1]);

        var firstElement = result[0] as JsonObject;
        var secondElement = result[1] as JsonObject;

        Assert.NotNull(firstElement);
        Assert.Equal("Alice", firstElement["name"]?.ToString());
        Assert.Equal("30", firstElement["age"]?.ToString());

        Assert.NotNull(secondElement);
        Assert.Equal("Bob", secondElement["name"]?.ToString());
        Assert.Equal("25", secondElement["age"]?.ToString());
    }

    [Fact]
    public void ConvertTo_JsonArrayToICollectionOfObject_ReturnsCollectionOfJsonObject()
    {
        // Arrange
        var jsonArrayString = "[{\"name\":\"Alice\",\"age\":30},{\"name\":\"Bob\",\"age\":25}]";
        var jsonArray = JsonNode.Parse(jsonArrayString);

        // Act
        var result = jsonArray.ConvertTo<ICollection<object>>(_objectConverterOptions);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result, item => Assert.IsType<JsonObject>(item));

        var firstElement = result.First() as JsonObject;
        var secondElement = result.Last() as JsonObject;

        Assert.NotNull(firstElement);
        Assert.Equal("Alice", firstElement["name"]?.ToString());
        Assert.Equal("30", firstElement["age"]?.ToString());

        Assert.NotNull(secondElement);
        Assert.Equal("Bob", secondElement["name"]?.ToString());
        Assert.Equal("25", secondElement["age"]?.ToString());
    }

    [Fact]
    public void ConvertFrom_JsonArrayToArrayOfComplexType_ReturnsArrayOfComplexType()
    {
        // Arrange
        var jsonArrayString = "[{\"name\":\"Alice\",\"age\":30},{\"name\":\"Bob\",\"age\":25}]";

        // Act
        var result = jsonArrayString.ConvertTo<Person[]>(_objectConverterOptions);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Length);
        Assert.Equal("Alice", result[0].Name);
        Assert.Equal("Bob", result[1].Name);
    }

    [Fact]
    public void ConvertFrom_ObjectArrayOfDoubleToArrayOfDouble_ReturnsArrayOfDouble()
    {
        // Arrange
        object[] objectArray = [1d, 2d, 3d];
        
        // Act
        var result = objectArray.ConvertTo<double[]>(_objectConverterOptions);
        
        // Assert
        Assert.NotNull(result);
    }

    [Theory]
    [InlineData("foo", false)]
    [InlineData("true", true)]
    [InlineData("false", false)]
    public void ConvertTo_StringToBool_StrictModeDisabled_ReturnsDefaultOrConverted(string input, bool expected)
    {
        var options = new ObjectConverterOptions(StrictMode: false);
        var result = input.ConvertTo<bool>(options);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ConvertTo_StringToBool_StrictModeEnabled_ThrowsException()
    {
        var options = new ObjectConverterOptions(StrictMode: true);
        Assert.Throws<TypeConversionException>(() => "foo".ConvertTo<bool>(options));
    }

    [Theory]
    [InlineData("bar", 0)]
    [InlineData("123", 123)]
    public void ConvertTo_StringToInt_StrictModeDisabled_ReturnsDefaultOrConverted(string input, int expected)
    {
        var options = new ObjectConverterOptions(StrictMode: false);
        var result = input.ConvertTo<int>(options);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ConvertTo_StringToInt_StrictModeEnabled_ThrowsException()
    {
        var options = new ObjectConverterOptions(StrictMode: true);
        Assert.Throws<TypeConversionException>(() => "bar".ConvertTo<int>(options));
    }

    [Theory]
    [InlineData("notadate", null!)]
    [InlineData("2023-01-01T00:00:00", "2023-01-01T00:00:00")]
    public void ConvertTo_StringToDateTime_StrictModeDisabled_ReturnsDefaultOrConverted(string input, string? expectedString)
    {
        var options = new ObjectConverterOptions(StrictMode: false);
        var result = input.ConvertTo<DateTime>(options);
        if (expectedString == null!)
            Assert.Equal(default, result);
        else
            Assert.Equal(DateTime.Parse(expectedString), result);
    }

    [Fact]
    public void ConvertTo_StringToDateTime_StrictModeEnabled_ThrowsException()
    {
        var options = new ObjectConverterOptions(StrictMode: true);
        Assert.Throws<TypeConversionException>(() => "notadate".ConvertTo<DateTime>(options));
    }
}