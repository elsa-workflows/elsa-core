using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.Expressions.Exceptions;
using Elsa.Expressions.Helpers;

namespace Elsa.Workflows.Core.UnitTests.ObjectConversion;

public class Tests
{
    [Fact]
    public void TryConvertTo_SameType_ReturnsSuccess()
    {
        // Arrange
        var value = 42;

        // Act
        var result = value.TryConvertTo<int>();

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
        var result = value.TryConvertTo<int>();

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
        var result = value.TryConvertTo<int>();

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
        var result = value.TryConvertTo<Dictionary<string, object>>();

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
        var result = value.ConvertTo<int>();

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void ConvertTo_JsonElementNumberToString_ReturnsString()
    {
        // Arrange
        var jsonElement = JsonNode.Parse("42").AsValue();

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
        var result = jsonNode.ConvertTo<ExpandoObject>();

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
        var result = value.ConvertTo<DateTime>();

        // Assert
        Assert.Equal(new(2023, 1, 1, 0, 0, 0), result);
    }

    [Fact]
    public void ConvertTo_StringToEnum_ReturnsEnum()
    {
        // Arrange
        var value = "Monday";

        // Act
        var result = value.ConvertTo<DayOfWeek>();

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
        var result = value.ConvertTo<byte[]>();

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
        Assert.Throws<TypeConversionException>(() => value.ConvertTo<Dictionary<string, object>>());
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
        var result = value.ConvertTo<List<int>>();

        // Assert
        Assert.Equal(new()
        {
            1,
            2,
            3
        }, result);
    }

    [Fact]
    public void ConvertTo_DateTimeToDateOnly_ReturnsDateOnly()
    {
        // Arrange
        var value = new DateTime(2023, 1, 1);

        // Act
        var result = value.ConvertTo<DateOnly>();

        // Assert
        Assert.Equal(new(2023, 1, 1), result);
    }

    [Fact]
    public void ConvertTo_DateOnlyToDateTime_ReturnsDateTime()
    {
        // Arrange
        var value = new DateOnly(2023, 1, 1);

        // Act
        var result = value.ConvertTo<DateTime>();

        // Assert
        Assert.Equal(new(2023, 1, 1, 0, 0, 0), result);
    }

    [Fact]
    public void ConvertTo_UnknownConversion_ThrowsInvalidCastException()
    {
        // Arrange
        var value = new object();

        // Act & Assert
        Assert.Throws<TypeConversionException>(() => value.ConvertTo<int>());
    }

    [Fact]
    public void ConvertTo_JsonArrayToListOfObject_ReturnsListOfJsonObject()
    {
        // Arrange
        var jsonArrayString = "[{\"name\":\"Alice\",\"age\":30},{\"name\":\"Bob\",\"age\":25}]";
        var jsonArray = JsonNode.Parse(jsonArrayString);
        var options = new ObjectConverterOptions();

        // Act
        var result = jsonArray.ConvertTo<List<object>>(options);

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
        var options = new ObjectConverterOptions();

        // Act
        var result = jsonArray.ConvertTo<ICollection<object>>(options);

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
    public void ConvertFrom_JsonArrayToArrayOfComplextType_ReturnsArrayOfComplexType()
    {
        // Arrange
        var jsonArrayString = "[{\"name\":\"Alice\",\"age\":30},{\"name\":\"Bob\",\"age\":25}]";
        var options = new ObjectConverterOptions();

        // Act
        var result = jsonArrayString.ConvertTo<Person[]>(options);

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
        var result = objectArray.ConvertTo<double[]>();
        
        // Assert
        Assert.NotNull(result);
    }
}