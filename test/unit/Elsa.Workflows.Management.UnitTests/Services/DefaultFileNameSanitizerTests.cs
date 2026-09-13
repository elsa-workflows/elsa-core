using Elsa.Workflows.Management.Services;

namespace Elsa.Workflows.Management.UnitTests.Services;

public class DefaultFileNameSanitizerTests
{
    private readonly DefaultFileNameSanitizer _sut = new();

    [Test]
    [Arguments("folder/child", "folder-child")]
    [Arguments("folder\\child", "folder-child")]
    [Arguments("already-safe", "already-safe")]
    public async Task Sanitize_Replaces_Path_Separators_And_Preserves_Safe_Names(string input, string expected)
    {
        var result = _sut.Sanitize(input);

        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    public async Task Sanitize_Replaces_Runtime_Invalid_File_Name_Characters()
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var hasNonSeparatorInvalidChar = invalidChars.Any(c => c is not '/' and not '\\');

        if (!hasNonSeparatorInvalidChar)
            return;

        var invalidCharacter = invalidChars.First(c => c is not '/' and not '\\');
        var result = _sut.Sanitize($"before{invalidCharacter}after");

        await Assert.That(result).IsEqualTo("before-after");
    }
}
