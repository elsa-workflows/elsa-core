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
    [Test]
    [DisplayName("String is preserved as-is")]
    public async Task StringIsPreservedAsIs()
    {
        var testString = "Hello World";
        var result = testString.Format();

        await Assert.That(result).IsEqualTo("Hello World");
    }

    [Test]
    [DisplayName("Byte array is serialized as base64 string")]
    public async Task ByteArrayIsSerializedAsBase64String()
    {
        var testByteArray = new byte[] { 0x01, 0x02, 0x03, 0x04, 0xFF };
        var result = testByteArray.Format();

        // Byte arrays are base64-encoded for serialization
        var formattedResult = await Assert.That(result).IsNotNull();
        var expectedBase64 = Convert.ToBase64String(testByteArray);
        await Assert.That(formattedResult).IsEqualTo(expectedBase64);
    }

    [Test]
    [DisplayName("Integer array is serialized as JSON array")]
    public async Task IntegerArrayIsSerializedAsJsonArray()
    {
        var testArray = new[] { 1, 2, 3, 4, 5 };
        var result = testArray.Format();

        await Assert.That(result).IsEqualTo("[1,2,3,4,5]");
    }

    [Test]
    [DisplayName("String array is serialized as JSON array")]
    public async Task StringArrayIsSerializedAsJsonArray()
    {
        var testArray = new[] { "Hello", "World" };
        var result = testArray.Format();

        await Assert.That(result).IsEqualTo("[\"Hello\",\"World\"]");
    }

    [Test]
    [DisplayName("String array with multiple elements is serialized as JSON array")]
    public async Task StringArrayWithMultipleElementsIsSerializedAsJsonArray()
    {
        var testArray = new[] { "Element 1", "Element 2", "Element 3" };
        var result = testArray.Format();

        await Assert.That(result).IsEqualTo("[\"Element 1\",\"Element 2\",\"Element 3\"]");
    }

    [Test]
    [DisplayName("Custom class array is serialized as JSON array")]
    public async Task CustomClassArrayIsSerializedAsJsonArray()
    {
        var testArray = new[] { new TestClass { Name = "Item1" }, new TestClass { Name = "Item2" } };
        var result = testArray.Format();

        // Should be JSON, not "TestClass[] Array"
        var formattedResult = await Assert.That(result).IsNotNull();
        await Assert.That(formattedResult).StartsWith("[").WithComparison(StringComparison.CurrentCulture);
        await Assert.That(formattedResult).Contains("Item1").WithComparison(StringComparison.CurrentCulture);
        await Assert.That(formattedResult).Contains("Item2").WithComparison(StringComparison.CurrentCulture);
    }

    [Test]
    [DisplayName("List of integers is serialized as JSON array")]
    public async Task ListOfIntegersIsSerializedAsJsonArray()
    {
        var testList = new List<int> { 1, 2, 3 };
        var result = testList.Format();

        await Assert.That(result).IsEqualTo("[1,2,3]");
    }

    [Test]
    [DisplayName("List with different values is serialized as JSON array")]
    public async Task ListWithDifferentValuesIsSerializedAsJsonArray()
    {
        var testList = new List<int> { 10, 20, 30 };
        var result = testList.Format();

        await Assert.That(result).IsEqualTo("[10,20,30]");
    }

    [Test]
    [DisplayName("Null returns null")]
    public async Task NullReturnsNull()
    {
        object? testValue = null;
        var result = testValue.Format();

        await Assert.That(result).IsNull();
    }

    private class TestClass
    {
        public string Name { get; set; } = string.Empty;
    }
}
