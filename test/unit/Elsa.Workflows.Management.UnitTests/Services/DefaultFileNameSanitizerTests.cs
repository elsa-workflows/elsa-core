using Elsa.Workflows.Management.Services;

namespace Elsa.Workflows.Management.UnitTests.Services;

public class DefaultFileNameSanitizerTests
{
    private readonly DefaultFileNameSanitizer _sut = new();

    [Theory]
    [InlineData("folder/child", "folder-child")]
    [InlineData("folder\\child", "folder-child")]
    [InlineData("already-safe", "already-safe")]
    public void Sanitize_Replaces_Path_Separators_And_Preserves_Safe_Names(string input, string expected)
    {
        var result = _sut.Sanitize(input);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Sanitize_Replaces_Runtime_Invalid_File_Name_Characters()
    {
        var invalidCharacter = Path.GetInvalidFileNameChars().FirstOrDefault(c => c is not '/' and not '\\');

        if (invalidCharacter == 0)
            return;

        var result = _sut.Sanitize($"before{invalidCharacter}after");

        Assert.Equal("before-after", result);
    }
}

