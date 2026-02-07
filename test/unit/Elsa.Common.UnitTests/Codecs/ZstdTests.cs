using Elsa.Common.Codecs;

namespace Elsa.Common.UnitTests.Codecs;

public class ZstdTests
{
    private readonly Zstd _codec = new();

    [Fact]
    public async Task CompressAsync_WithSimpleString_ReturnsCompressedString()
    {
        // Arrange
        var input = "Hello, World!";

        // Act
        var result = await _codec.CompressAsync(input);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.NotEqual(input, result);
    }

    [Theory]
    [InlineData("Hello, World!")]
    [InlineData("")]
    [InlineData("Hello! 你好! مرحبا! Здравствуйте! 🎉🎊")]
    [InlineData("{\"name\":\"John Doe\",\"age\":30,\"city\":\"New York\",\"items\":[1,2,3,4,5]}")]
    public async Task CompressDecompress_RoundTrip_PreservesOriginalData(string original)
    {
        // Act
        var compressed = await _codec.CompressAsync(original);
        var decompressed = await _codec.DecompressAsync(compressed);

        // Assert
        Assert.Equal(original, decompressed);
    }

    [Fact]
    public async Task CompressDecompress_WithLargeString_WorksCorrectly()
    {
        // Arrange
        var original = string.Join("", Enumerable.Repeat("This is a test string that will be compressed. ", 1000));

        // Act
        var compressed = await _codec.CompressAsync(original);
        var decompressed = await _codec.DecompressAsync(compressed);

        // Assert
        Assert.Equal(original, decompressed);
        Assert.True(compressed.Length < original.Length, "Compressed string should be smaller than original");
    }

    [Fact]
    public async Task CompressAsync_MultipleCallsWithSameInput_ProducesConsistentResults()
    {
        // Arrange
        var input = "Test string for consistency";

        // Act
        var result1 = await _codec.CompressAsync(input);
        var result2 = await _codec.CompressAsync(input);

        // Assert
        Assert.Equal(result1, result2);
    }
}
