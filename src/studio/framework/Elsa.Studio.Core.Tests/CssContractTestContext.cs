using Xunit;

namespace Elsa.Studio.Core.Tests;

internal static class CssContractTestContext
{
    public static string ReadRepositoryFile(params string[] pathSegments) =>
        File.ReadAllText(Path.Combine([FindRepositoryRoot(), .. pathSegments]));

    public static string GetRuleBody(string css, string selector)
    {
        var ruleStart = css.IndexOf($"{selector} {{", StringComparison.Ordinal);
        Assert.True(ruleStart >= 0, $"Expected CSS rule '{selector}'.");

        var bodyStart = ruleStart + selector.Length + 2;
        var bodyEnd = css.IndexOf('}', bodyStart);
        Assert.True(bodyEnd >= 0, $"Expected CSS rule '{selector}' to have a closing brace.");
        return css[bodyStart..bodyEnd];
    }

    private static string FindRepositoryRoot()
    {
        for (var current = new DirectoryInfo(AppContext.BaseDirectory); current is not null; current = current.Parent)
        {
            if (Directory.Exists(Path.Combine(current.FullName, "src", "framework", "Elsa.Studio.Shell")))
                return current.FullName;
        }

        throw new DirectoryNotFoundException("Could not locate the Elsa Studio repository root.");
    }
}
