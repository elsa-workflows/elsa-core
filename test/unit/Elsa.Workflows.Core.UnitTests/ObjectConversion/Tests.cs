using System.ComponentModel;
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
        Assert.True(result.IsSuccess);
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
        Assert.True(result.IsSuccess);
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
        Assert.False(result.IsSuccess);
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
        Assert.False(result.IsSuccess);
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
    [InlineData("foo", false, typeof(bool))]
    [InlineData("true", true, typeof(bool))]
    [InlineData("false", false, typeof(bool))]
    [InlineData("bar", 0, typeof(int))]
    [InlineData("123", 123, typeof(int))]
    [InlineData("notadate", null!, typeof(DateTime?))]
    [InlineData("2023-01-01T00:00:00", "2023-01-01T00:00:00", typeof(DateTime))]
    public void ConvertTo_StrictModeDisabled_ReturnsDefaultOrConverted(string input, object? expected, Type targetType)
    {
        var options = new ObjectConverterOptions(StrictMode: false);
        var result = input.ConvertTo(targetType, options);
        
        if (expected is null)
        {
            Assert.Null(result);
            return;
        }
        
        // Special handling for DateTime
        if (targetType == typeof(DateTime) && expected is string expectedString)
        {
            var expectedDateTime = DateTime.Parse(expectedString, null, System.Globalization.DateTimeStyles.RoundtripKind);
            Assert.Equal(expectedDateTime, result);
            return;
        }
        
        Assert.Equal(expected, result);
    }
    
    [Theory]
    [InlineData("foo", true, typeof(bool))]
    [InlineData("true", false, typeof(bool))]
    [InlineData("false", false, typeof(bool))]
    [InlineData("bar", true, typeof(int))]
    [InlineData("123", false, typeof(int))]
    [InlineData("notadate", true, typeof(DateTime?))]
    [InlineData("2023-01-01T00:00:00", false, typeof(DateTime))]
    public void ConvertTo_StrictModeEnabled_Throws(string input, bool shouldThrow, Type targetType)
    {
        var options = new ObjectConverterOptions(StrictMode: true);
        
        if(shouldThrow)
            Assert.Throws<TypeConversionException>(() => input.ConvertTo(targetType, options));    
    }

    [Fact]
    public void ConvertTo_WithRegisteredPersonTypeConverter_ConvertsFromJsonToPerson()
    {
        try
        {
            // Arrange
            TypeDescriptor.AddAttributes(typeof(Person), new TypeConverterAttribute(typeof(PersonTypeConverter)));

            var json = "{\"Name\":\"Alice\",\"Age\":30}";

            // Act
            var result = json.ConvertTo<Person>(_objectConverterOptions);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<Person>(result);
            Assert.Equal("Alice", result.Name);
            Assert.Equal(30, result.Age);
        }
        finally
        {
            // Clean up type descriptor cache so other tests aren't affected
            TypeDescriptor.Refresh(typeof(Person));
        }
    }

    [Fact]
    public void ConvertTo_WithRegisteredPersonTypeConverter_NullInput_ReturnsNull()
    {
        try
        {
            // Arrange
            TypeDescriptor.AddAttributes(typeof(Person), new TypeConverterAttribute(typeof(PersonTypeConverter)));

            string? json = null;

            // Act
            var result = json.ConvertTo<Person>(_objectConverterOptions);

            // Assert
            Assert.Null(result);
        }
        finally
        {
            TypeDescriptor.Refresh(typeof(Person));
        }
    }

    [Fact]
    public void ConvertTo_WithRegisteredPersonTypeConverter_ConvertsFromPersonToJson()
    {
        try
        {
            // Arrange
            TypeDescriptor.AddAttributes(typeof(Person), new TypeConverterAttribute(typeof(PersonTypeConverter)));

            var person = new Person { Name = "Bob", Age = 42 };

            // Act
            var result = person.ConvertTo<string>(_objectConverterOptions);

            // Assert
            Assert.NotNull(result);
            var json = Assert.IsType<string>(result);
            Assert.Contains("\"Name\":\"Bob\"", json);
            Assert.Contains("\"Age\":42", json);
        }
        finally
        {
            // Clean up to avoid polluting TypeDescriptor globally
            TypeDescriptor.Refresh(typeof(Person));
        }
    }

    [Fact]
    public void ConvertTo_WithRegisteredPersonTypeConverter_NullPersonToString_ReturnsNull()
    {
        try
        {
            // Arrange
            TypeDescriptor.AddAttributes(typeof(Person), new TypeConverterAttribute(typeof(PersonTypeConverter)));
            Person? person = null;

            // Act
            var result = person.ConvertTo<string>(_objectConverterOptions);

            // Assert
            Assert.Null(result);
        }
        finally
        {
            TypeDescriptor.Refresh(typeof(Person));
        }
    }
}