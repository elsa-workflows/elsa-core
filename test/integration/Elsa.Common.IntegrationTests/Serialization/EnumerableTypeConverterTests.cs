using Elsa.Common.Serialization;

namespace Elsa.Common.IntegrationTests.Serialization;

public class EnumerableTypeConverterTests
{
    [Fact(DisplayName = "String variable is not serialized as JSON array")]
    public void StringVariableIsNotSerializedAsJsonArray()
    {
        var testString = "Hello World";
        AssertTypeConverterPreservesValue(testString);
    }

    [Fact(DisplayName = "Byte array variable is not serialized as JSON array")]
    public void ByteArrayVariableIsNotSerializedAsJsonArray()
    {
        var testByteArray = new byte[] { 0x01, 0x02, 0x03, 0x04, 0xFF };
        AssertTypeConverterPreservesValue(testByteArray);
    }

    [Fact(DisplayName = "String array is serialized as JSON array")]
    public void StringArrayIsSerializedAsJsonArray()
    {
        var testArray = new[] { "Element 1", "Element 2", "Element 3" };
        AssertTypeConverterSerializesToJson(testArray, "[\"Element 1\",\"Element 2\",\"Element 3\"]");
    }

    [Fact(DisplayName = "List is serialized as JSON array")]
    public void ListIsSerializedAsJsonArray()
    {
        var testList = new List<int> { 1, 2, 3 };
        AssertTypeConverterSerializesToJson(testList, "[1,2,3]");
    }

    private void AssertTypeConverterPreservesValue<T>(T expectedValue)
    {
        var converter = new EnumerableTypeConverter();
        var result = converter.ConvertTo(null, null, expectedValue, typeof(string));

        Assert.Equal(expectedValue, result);
        Assert.IsType<T>(result);
    }

    private void AssertTypeConverterSerializesToJson<T>(T value, string expectedJson)
    {
        var converter = new EnumerableTypeConverter();
        var result = converter.ConvertTo(null, null, value, typeof(string));

        Assert.IsType<string>(result);
        Assert.Equal(expectedJson, result);
    }
}
