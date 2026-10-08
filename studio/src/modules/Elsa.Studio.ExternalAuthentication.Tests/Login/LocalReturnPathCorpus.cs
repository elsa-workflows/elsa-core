using Xunit;

namespace Elsa.Studio.ExternalAuthentication.Tests.Login;

/// <summary>
/// Shared table-driven return-path cases used by LocalReturnPath, OIDC, WASM, and chooser tests.
/// </summary>
public static class LocalReturnPathCorpus
{
    public static TheoryData<string?, string> Cases { get; } = CreateCases();

    public static string EncodeTimes(string value, int times)
    {
        var current = value;
        for (var i = 0; i < times; i++)
        {
            current = Uri.EscapeDataString(current);
        }

        return current;
    }

    private static TheoryData<string?, string> CreateCases()
    {
        var cases = new TheoryData<string?, string>
        {
            { null, "/" },
            { "", "/" },
            { "/%09/evil.com", "/" },
            { "//evil.com", "/" },
            { "/\\evil.com", "/" },
            { "\\\\evil.com", "/" },
            { "/%2F/evil.com", "/" },
            { "http://evil.com", "/" },
            { "https://evil.com/phish", "/" },
            { "javascript:alert(1)", "/" },
            { "http:evil.com", "/" },
            { "javascript%3Aalert(1)", "/" },
            { "%6Aavascript:alert(1)", "/" },
            { "\t//evil.com", "/" },
            { "c|/windows", "/" },
            { "c|evil.com", "/" },
            { " //evil.com", "/" },
            { "workflows/x", "/" },
            { "workflows/definitions?tab=active", "/" },
            { "workflows/instances", "/" },
            { "/workflows/definitions?x=1", "/workflows/definitions?x=1" },
            { "/workflows?version=1", "/workflows?version=1" },
            { "/caf%C3%A9", "/caf%C3%A9" },
            { "/a?x=%26y", "/a?x=%26y" },
            { "/a?ref=https%3A%2F%2Fexample.com", "/a?ref=https%3A%2F%2Fexample.com" },
            { "/a%2Fb", "/a%2Fb" },
            { "/a?ref=https://example.com", "/a?ref=https://example.com" }
        };

        cases.Add(EncodeTimes("//evil.com", 9), "/");
        cases.Add(EncodeTimes("//", 9), "/");
        cases.Add("/" + EncodeTimes("//evil.com", 9), "/");

        return cases;
    }
}
