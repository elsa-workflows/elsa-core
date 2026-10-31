namespace Elsa.Platform.Integration.Models;

public sealed class PlatformRecipeArtifact(IReadOnlyDictionary<string, string> textEntries)
{
    private readonly IReadOnlyDictionary<string, string> _textEntries = textEntries;

    public bool TryGetText(string path, out string content)
    {
        var normalizedPath = NormalizePath(path);
        return _textEntries.TryGetValue(normalizedPath, out content!);
    }

    private static string NormalizePath(string path) =>
        path.Replace('\\', '/').TrimStart('/');
}
