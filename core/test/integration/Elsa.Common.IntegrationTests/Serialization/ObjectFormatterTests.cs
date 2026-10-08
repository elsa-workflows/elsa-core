using Elsa.Expressions.Helpers;

namespace Elsa.Common.IntegrationTests.Serialization;

/// <summary>
/// Tests for ObjectFormatter.Format() to ensure proper serialization of different types:
/// - Strings are preserved as-is
/// - Byte arrays are base64-encoded
/// - Arrays and collections serialize to JSON instead of "T[] Array"
/// Related to GitHub issue #7019.
/// </summary>
public class ObjectFormatterTests
{
    [Fact(DisplayName = "String is preserved as-is")]
    public void StringIsPreservedAsIs()
    {
        var testString = "Hello World";
        var result = testString.Format();

        Assert.Equal("Hello World", result);
    }

    [Fact(DisplayName = "Byte array is serialized as base64 string")]
    public void ByteArrayIsSerializedAsBase64String()
    {
        var testByteArray = new byte[] { 0x01, 0x02, 0x03, 0x04, 0xFF };
        var result = testByteArray.Format();

        // Byte arrays are base64-encoded for serialization
        Assert.NotNull(result);
        var expectedBase64 = Convert.ToBase64String(testByteArray);
        Assert.Equal(expectedBase64, result);
    }

    [Fact(DisplayName = "Integer array is serialized as JSON array")]
    public void IntegerArrayIsSerializedAsJsonArray()
    {
        var testArray = new[] { 1, 2, 3, 4, 5 };
        var result = testArray.Format();

        Assert.Equal("[1,2,3,4,5]", result);
    }

    [Fact(DisplayName = "String array is serialized as JSON array")]
    public void StringArrayIsSerializedAsJsonArray()
    {
        var testArray = new[] { "Hello", "World" };
        var result = testArray.Format();

        Assert.Equal("[\"Hello\",\"World\"]", result);
    }

    [Fact(DisplayName = "String array with multiple elements is serialized as JSON array")]
    public void StringArrayWithMultipleElementsIsSerializedAsJsonArray()
    {
        var testArray = new[] { "Element 1", "Element 2", "Element 3" };
        var result = testArray.Format();

        Assert.Equal("[\"Element 1\",\"Element 2\",\"Element 3\"]", result);
    }

    [Fact(DisplayName = "Custom class array is serialized as JSON array")]
    public void CustomClassArrayIsSerializedAsJsonArray()
    {
        var testArray = new[] { new TestClass { Name = "Item1" }, new TestClass { Name = "Item2" } };
        var result = testArray.Format();

        // Should be JSON, not "TestClass[] Array"
        Assert.NotNull(result);
        Assert.StartsWith("[", result);
        Assert.Contains("Item1", result);
        Assert.Contains("Item2", result);
    }

    [Fact(DisplayName = "List of integers is serialized as JSON array")]
    public void ListOfIntegersIsSerializedAsJsonArray()
    {
        var testList = new List<int> { 1, 2, 3 };
        var result = testList.Format();

        Assert.Equal("[1,2,3]", result);
    }

    [Fact(DisplayName = "List with different values is serialized as JSON array")]
    public void ListWithDifferentValuesIsSerializedAsJsonArray()
    {
        var testList = new List<int> { 10, 20, 30 };
        var result = testList.Format();

        Assert.Equal("[10,20,30]", result);
    }

    [Fact(DisplayName = "Null returns null")]
    public void NullReturnsNull()
    {
        object? testValue = null;
        var result = testValue.Format();

        Assert.Null(result);
    }

    private class TestClass
    {
        public string Name { get; set; } = string.Empty;
    }
}

