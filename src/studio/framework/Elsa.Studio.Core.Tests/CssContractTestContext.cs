using Xunit;

namespace Elsa.Studio.Core.Tests;

internal static class CssContractTestContext
{
    public static string ReadRepositoryFile(params string[] pathSegments)
    {
        if (pathSegments.Length < 2 || pathSegments[0] != "src")
            throw new ArgumentException("Expected a Studio source path beginning with src.", nameof(pathSegments));

        for (var current = new DirectoryInfo(AppContext.BaseDirectory); current is not null; current = current.Parent)
        {
            var standalonePath = Path.Combine([current.FullName, .. pathSegments]);
            if (File.Exists(standalonePath))
                return File.ReadAllText(standalonePath);

            var consolidatedPath = Path.Combine([current.FullName, "src", "studio", .. pathSegments[1..]]);
            if (File.Exists(consolidatedPath))
                return File.ReadAllText(consolidatedPath);
        }

        throw new FileNotFoundException($"Could not locate Studio source file '{Path.Combine(pathSegments)}'.");
    }

    public static string GetRuleBody(string css, string selector)
    {
        var ruleStart = css.IndexOf($"{selector} {{", StringComparison.Ordinal);
        Assert.True(ruleStart >= 0, $"Expected CSS rule '{selector}'.");

        var bodyStart = ruleStart + selector.Length + 2;
        var bodyEnd = css.IndexOf('}', bodyStart);
        Assert.True(bodyEnd >= 0, $"Expected CSS rule '{selector}' to have a closing brace.");
        return css[bodyStart..bodyEnd];
    }

}
