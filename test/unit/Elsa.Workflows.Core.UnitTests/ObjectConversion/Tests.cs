using System.ComponentModel;
using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.Expressions.Exceptions;
using Elsa.Expressions.Helpers;
using System.Threading.Tasks;

namespace Elsa.Workflows.Core.UnitTests.ObjectConversion;

public class Tests
{
    private readonly ObjectConverterOptions _objectConverterOptions = new(StrictMode: true);

    [Test]
    public async Task TryConvertTo_SameType_ReturnsSuccess()
    {
        // Arrange
        var value = 42;

        // Act
        var result = value.TryConvertTo<int>(_objectConverterOptions);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsEqualTo(42);
    }

    [Test]
    public async Task TryConvertTo_DifferentType_ReturnsConvertedValue()
    {
        // Arrange
        var value = "42";

        // Act
        var result = value.TryConvertTo<int>(_objectConverterOptions);

        // Assert
        await Assert.That(result.IsSuccess).IsTrue();
        await Assert.That(result.Value).IsEqualTo(42);
    }

    [Test]
    public async Task TryConvertTo_InvalidConversion_ReturnsFailure()
    {
        // Arrange
        var value = "invalid";

        // Act
        var result = value.TryConvertTo<int>(_objectConverterOptions);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Exception).IsNotNull();
    }

    [Test]
    public async Task TryConvertTo_InvalidJsonString_ReturnsFailure()
    {
        // Arrange
        var value = "{ invalid json }";

        // Act
        var result = value.TryConvertTo<Dictionary<string, object>>(_objectConverterOptions);

        // Assert
        await Assert.That(result.IsSuccess).IsFalse();
        await Assert.That(result.Exception).IsNotNull();
    }

    [Test]
    public async Task ConvertTo_NullValue_ReturnsDefault()
    {
        // Arrange
        object? value = null;

        // Act
        var result = value.ConvertTo<int>(_objectConverterOptions);

        // Assert
        await Assert.That(result).IsEqualTo(0);
    }

    [Test]
    public async Task ConvertTo_JsonElementNumberToString_ReturnsString()
    {
        // Arrange
        var jsonElement = JsonNode.Parse("42")!.AsValue();

        // Act
        var result = jsonElement.ConvertTo<string>();

        // Assert
        await Assert.That(result).IsEqualTo("42");
    }

    [Test]
    public async Task ConvertTo_JsonNodeToExpandoObject_ReturnsExpandoObject()
    {
        // Arrange
        var jsonNode = JsonNode.Parse("{ \"key\": \"value\" }");

        // Act
        var result = jsonNode.ConvertTo<ExpandoObject>(_objectConverterOptions);

        // Assert
        dynamic expando = (await Assert.That(result).IsTypeOf<ExpandoObject>())!;
        object value = expando.key;

        // This is not the result I expect, I would have expected the JsonNode to have been recursively converted

        await Assert.That(value is JsonElement).IsTrue();
        await Assert.That(((JsonElement)value).ValueKind).IsEqualTo(JsonValueKind.String);
        await Assert.That(((JsonElement)value).GetString()).IsEqualTo("value");
    }

    [Test]
    public async Task ConvertTo_StringToDateTime_ReturnsDateTime()
    {
        // Arrange
        var value = "2023-01-01T00:00:00";

        // Act
        var result = value.ConvertTo<DateTime>(_objectConverterOptions);

        // Assert
        await Assert.That(result).IsEqualTo(new(2023, 1, 1, 0, 0, 0));
    }

    [Test]
    public async Task ConvertTo_StringToEnum_ReturnsEnum()
    {
        // Arrange
        var value = "Monday";

        // Act
        var result = value.ConvertTo<DayOfWeek>(_objectConverterOptions);

        // Assert
        await Assert.That(result).IsEqualTo(DayOfWeek.Monday);
    }

    [Test]
    public async Task ConvertTo_StringToByteArray_ReturnsByteArray()
    {
        // Arrange
        var value = Convert.ToBase64String(new byte[]
        {
            1, 2, 3
        });

        // Act
        var result = value.ConvertTo<byte[]>(_objectConverterOptions);

        // Assert
        await Assert.That(result).IsEquivalentTo(new byte[]
        {
            1, 2, 3
        }, TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    public void ConvertTo_InvalidJsonString_ThrowsException()
    {
        // Arrange
        var value = "{ invalid json }";

        // Act & Assert
        Assert.ThrowsExactly<TypeConversionException>(() => value.ConvertTo<Dictionary<string, object>>(_objectConverterOptions));
    }

    [Test]
    public async Task ConvertTo_EnumerableToList_ReturnsConvertedList()
    {
        // Arrange
        var value = new[]
        {
            "1", "2", "3"
        };

        // Act
        var result = value.ConvertTo<List<int>>(_objectConverterOptions);

        // Assert
        await Assert.That(result).IsEquivalentTo([
            1,
            2,
            3
        ], TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    public async Task ConvertTo_EnumerableToHashSet_ReturnsConvertedHashSet()
    {
        // Arrange
        var value = new[]
        {
            "1", "2", "2", "3"
        };

        // Act
        var result = value.ConvertTo<HashSet<int>>(_objectConverterOptions);

        // Assert
        await Assert.That(result).IsTypeOf<HashSet<int>>();
        await Assert.That(result!.SetEquals([1, 2, 3])).IsTrue();
    }

    [Test]
    public async Task ConvertTo_DateTimeToDateOnly_ReturnsDateOnly()
    {
        // Arrange
        var value = new DateTime(2023, 1, 1);

        // Act
        var result = value.ConvertTo<DateOnly>(_objectConverterOptions);

        // Assert
        await Assert.That(result).IsEqualTo(new(2023, 1, 1));
    }

    [Test]
    public async Task ConvertTo_DateOnlyToDateTime_ReturnsDateTime()
    {
        // Arrange
        var value = new DateOnly(2023, 1, 1);

        // Act
        var result = value.ConvertTo<DateTime>(_objectConverterOptions);

        // Assert
        await Assert.That(result).IsEqualTo(new(2023, 1, 1, 0, 0, 0));
    }

    [Test]
    public void ConvertTo_UnknownConversion_ThrowsInvalidCastException()
    {
        // Arrange
        var value = new object();

        // Act & Assert
        Assert.ThrowsExactly<TypeConversionException>(() => value.ConvertTo<int>(_objectConverterOptions));
    }

    [Test]
    public async Task ConvertTo_JsonArrayToListOfObject_ReturnsListOfJsonObject()
    {
        // Arrange
        var jsonArrayString = "[{\"name\":\"Alice\",\"age\":30},{\"name\":\"Bob\",\"age\":25}]";
        var jsonArray = JsonNode.Parse(jsonArrayString);

        // Act
        var result = jsonArray.ConvertTo<List<object>>(_objectConverterOptions);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Count).IsEqualTo(2);
        await Assert.That(result[0]).IsTypeOf<JsonObject>();
        await Assert.That(result[1]).IsTypeOf<JsonObject>();

        var firstElement = result[0] as JsonObject;
        var secondElement = result[1] as JsonObject;

        await Assert.That(firstElement).IsNotNull();
        await Assert.That(firstElement["name"]?.ToString()).IsEqualTo("Alice");
        await Assert.That(firstElement["age"]?.ToString()).IsEqualTo("30");

        await Assert.That(secondElement).IsNotNull();
        await Assert.That(secondElement["name"]?.ToString()).IsEqualTo("Bob");
        await Assert.That(secondElement["age"]?.ToString()).IsEqualTo("25");
    }

    [Test]
    public async Task ConvertTo_JsonArrayToICollectionOfObject_ReturnsCollectionOfJsonObject()
    {
        // Arrange
        var jsonArrayString = "[{\"name\":\"Alice\",\"age\":30},{\"name\":\"Bob\",\"age\":25}]";
        var jsonArray = JsonNode.Parse(jsonArrayString);

        // Act
        var result = jsonArray.ConvertTo<ICollection<object>>(_objectConverterOptions);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Count).IsEqualTo(2);
        foreach (var item in result)
            await Assert.That(item).IsTypeOf<JsonObject>();

        var firstElement = result.First() as JsonObject;
        var secondElement = result.Last() as JsonObject;

        await Assert.That(firstElement).IsNotNull();
        await Assert.That(firstElement["name"]?.ToString()).IsEqualTo("Alice");
        await Assert.That(firstElement["age"]?.ToString()).IsEqualTo("30");

        await Assert.That(secondElement).IsNotNull();
        await Assert.That(secondElement["name"]?.ToString()).IsEqualTo("Bob");
        await Assert.That(secondElement["age"]?.ToString()).IsEqualTo("25");
    }

    [Test]
    public async Task ConvertFrom_JsonArrayToArrayOfComplexType_ReturnsArrayOfComplexType()
    {
        // Arrange
        var jsonArrayString = "[{\"name\":\"Alice\",\"age\":30},{\"name\":\"Bob\",\"age\":25}]";

        // Act
        var result = jsonArrayString.ConvertTo<Person[]>(_objectConverterOptions);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Length).IsEqualTo(2);
        await Assert.That(result[0].Name).IsEqualTo("Alice");
        await Assert.That(result[1].Name).IsEqualTo("Bob");
    }

    [Test]
    public async Task ConvertFrom_ObjectArrayOfDoubleToArrayOfDouble_ReturnsArrayOfDouble()
    {
        // Arrange
        object[] objectArray = [1d, 2d, 3d];

        // Act
        var result = objectArray.ConvertTo<double[]>(_objectConverterOptions);

        // Assert
        await Assert.That(result).IsNotNull();
    }

    [Test]
    [Arguments("foo", false, typeof(bool))]
    [Arguments("true", true, typeof(bool))]
    [Arguments("false", false, typeof(bool))]
    [Arguments("bar", 0, typeof(int))]
    [Arguments("123", 123, typeof(int))]
    [Arguments("notadate", null!, typeof(DateTime?))]
    [Arguments("2023-01-01T00:00:00", "2023-01-01T00:00:00", typeof(DateTime))]
    public async Task ConvertTo_StrictModeDisabled_ReturnsDefaultOrConverted(string input, object? expected, Type targetType)
    {
        var options = new ObjectConverterOptions(StrictMode: false);
        var result = input.ConvertTo(targetType, options);

        if (expected is null)
        {
            await Assert.That(result).IsNull();
            return;
        }

        // Special handling for DateTime
        if (targetType == typeof(DateTime) && expected is string expectedString)
        {
            var expectedDateTime = DateTime.Parse(expectedString, null, System.Globalization.DateTimeStyles.RoundtripKind);
            await Assert.That(result).IsEqualTo(expectedDateTime);
            return;
        }

        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    [Arguments("foo", true, typeof(bool))]
    [Arguments("true", false, typeof(bool))]
    [Arguments("false", false, typeof(bool))]
    [Arguments("bar", true, typeof(int))]
    [Arguments("123", false, typeof(int))]
    [Arguments("notadate", true, typeof(DateTime?))]
    [Arguments("2023-01-01T00:00:00", false, typeof(DateTime))]
    public void ConvertTo_StrictModeEnabled_Throws(string input, bool shouldThrow, Type targetType)
    {
        var options = new ObjectConverterOptions(StrictMode: true);

        if (shouldThrow)
            Assert.ThrowsExactly<TypeConversionException>(() => input.ConvertTo(targetType, options));
    }

    [Test]
    public async Task ConvertTo_WithRegisteredPersonTypeConverter_ConvertsFromJsonToPerson()
    {
        try
        {
            // Arrange
            TypeDescriptor.AddAttributes(typeof(Person), new TypeConverterAttribute(typeof(PersonTypeConverter)));

            var json = "{\"Name\":\"Alice\",\"Age\":30}";

            // Act
            var result = json.ConvertTo<Person>(_objectConverterOptions);

            // Assert
            await Assert.That(result).IsNotNull();
            await Assert.That(result).IsTypeOf<Person>();
            await Assert.That(result.Name).IsEqualTo("Alice");
            await Assert.That(result.Age).IsEqualTo(30);
        }
        finally
        {
            // Clean up type descriptor cache so other tests aren't affected
            TypeDescriptor.Refresh(typeof(Person));
        }
    }

    [Test]
    public async Task ConvertTo_WithRegisteredPersonTypeConverter_NullInput_ReturnsNull()
    {
        try
        {
            // Arrange
            TypeDescriptor.AddAttributes(typeof(Person), new TypeConverterAttribute(typeof(PersonTypeConverter)));

            string? json = null;

            // Act
            var result = json.ConvertTo<Person>(_objectConverterOptions);

            // Assert
            await Assert.That(result).IsNull();
        }
        finally
        {
            TypeDescriptor.Refresh(typeof(Person));
        }
    }

    [Test]
    public async Task ConvertTo_WithRegisteredPersonTypeConverter_ConvertsFromPersonToJson()
    {
        try
        {
            // Arrange
            TypeDescriptor.AddAttributes(typeof(Person), new TypeConverterAttribute(typeof(PersonTypeConverter)));

            var person = new Person { Name = "Bob", Age = 42 };

            // Act
            var result = person.ConvertTo<string>(_objectConverterOptions);

            // Assert
            await Assert.That(result).IsNotNull();
            var json = await Assert.That(result).IsTypeOf<string>();
            await Assert.That(json).Contains("\"Name\":\"Bob\"");
            await Assert.That(json).Contains("\"Age\":42");
        }
        finally
        {
            // Clean up to avoid polluting TypeDescriptor globally
            TypeDescriptor.Refresh(typeof(Person));
        }
    }

    [Test]
    public async Task ConvertTo_WithRegisteredPersonTypeConverter_NullPersonToString_ReturnsNull()
    {
        try
        {
            // Arrange
            TypeDescriptor.AddAttributes(typeof(Person), new TypeConverterAttribute(typeof(PersonTypeConverter)));
            Person? person = null;

            // Act
            var result = person.ConvertTo<string>(_objectConverterOptions);

            // Assert
            await Assert.That(result).IsNull();
        }
        finally
        {
            TypeDescriptor.Refresh(typeof(Person));
        }
    }
}
