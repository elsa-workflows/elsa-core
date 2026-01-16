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

    [Fact]
    public async Task DecompressAsync_WithCompressedString_ReturnsOriginalString()
    {
        // Arrange
        var original = "Hello, World!";
        var compressed = await _codec.CompressAsync(original);

        // Act
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
    public async Task CompressDecompress_WithEmptyString_WorksCorrectly()
    {
        // Arrange
        var original = string.Empty;

        // Act
        var compressed = await _codec.CompressAsync(original);
        var decompressed = await _codec.DecompressAsync(compressed);

        // Assert
        Assert.Equal(original, decompressed);
    }

    [Fact]
    public async Task CompressDecompress_WithSpecialCharacters_WorksCorrectly()
    {
        // Arrange
        var original = "Hello! 你好! مرحبا! Здравствуйте! 🎉🎊";

        // Act
        var compressed = await _codec.CompressAsync(original);
        var decompressed = await _codec.DecompressAsync(compressed);

        // Assert
        Assert.Equal(original, decompressed);
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

    [Fact]
    public async Task CompressDecompress_WithJsonData_WorksCorrectly()
    {
        // Arrange
        var original = "{\"name\":\"John Doe\",\"age\":30,\"city\":\"New York\",\"items\":[1,2,3,4,5]}";

        // Act
        var compressed = await _codec.CompressAsync(original);
        var decompressed = await _codec.DecompressAsync(compressed);

        // Assert
        Assert.Equal(original, decompressed);
    }
}
