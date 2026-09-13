using Elsa.Common.Codecs;
using System.Threading.Tasks;

namespace Elsa.Common.UnitTests.Codecs;

public class ZstdTests
{
    private readonly Zstd _codec = new();

    [Test]
    public async Task CompressAsync_WithSimpleString_ReturnsCompressedString()
    {
        // Arrange
        var input = "Hello, World!";

        // Act
        var result = await _codec.CompressAsync(input);

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result).IsNotEmpty();
        await Assert.That(result).IsNotEqualTo(input);
    }

    [Test]
    [Arguments("Hello, World!")]
    [Arguments("")]
    [Arguments("Hello! 你好! مرحبا! Здравствуйте! 🎉🎊")]
    [Arguments("{\"name\":\"John Doe\",\"age\":30,\"city\":\"New York\",\"items\":[1,2,3,4,5]}")]
    public async Task CompressDecompress_RoundTrip_PreservesOriginalData(string original)
    {
        // Act
        var compressed = await _codec.CompressAsync(original);
        var decompressed = await _codec.DecompressAsync(compressed);

        // Assert
        await Assert.That(decompressed).IsEquivalentTo(original, TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }

    [Test]
    public async Task CompressDecompress_WithLargeString_WorksCorrectly()
    {
        // Arrange
        var original = string.Join("", Enumerable.Repeat("This is a test string that will be compressed. ", 1000));

        // Act
        var compressed = await _codec.CompressAsync(original);
        var decompressed = await _codec.DecompressAsync(compressed);

        // Assert
        await Assert.That(decompressed).IsEquivalentTo(original, TUnit.Assertions.Enums.CollectionOrdering.Matching);
        await Assert.That(compressed.Length < original.Length).IsTrue().Because("Compressed string should be smaller than original");
    }

    [Test]
    public async Task CompressAsync_MultipleCallsWithSameInput_ProducesConsistentResults()
    {
        // Arrange
        var input = "Test string for consistency";

        // Act
        var result1 = await _codec.CompressAsync(input);
        var result2 = await _codec.CompressAsync(input);

        // Assert
        await Assert.That(result2).IsEquivalentTo(result1, TUnit.Assertions.Enums.CollectionOrdering.Matching);
    }
}
