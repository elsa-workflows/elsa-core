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
            { "https://attacker.example", "/" },
            { "http://attacker.example", "/" },
            { "https://evil.example/", "/" },
            { "//attacker.example", "/" },
            { "/\\attacker.example", "/" },
            { "javascript:alert(1)", "/" },
            { " //evil.com", "/" },
            { "/workflows/definitions?x=1", "/workflows/definitions?x=1" },
            { "/workflows?version=1", "/workflows?version=1" },
            { "/workflows?tab=active", "/workflows?tab=active" },
            { "/caf%C3%A9", "/caf%C3%A9" },
            { "/a?x=%26y", "/a?x=%26y" },
            { "/a?ref=https%3A%2F%2Fexample.com", "/a?ref=https%3A%2F%2Fexample.com" },
            { "/a%2Fb", "/a%2Fb" },
            { "/a?ref=https://example.com", "/a?ref=https://example.com" },
            { "/workflows?documentation=https://example.com", "/workflows?documentation=https://example.com" },
            { "/workflows?filter=red%26blue", "/workflows?filter=red%26blue" },
            { "/workflows?name=a%26b", "/workflows?name=a%26b" },
            { "/workflows?filter=a%26b", "/workflows?filter=a%26b" },
            { "workflows/x", "workflows/x" },
            { "workflows/definitions?tab=active", "workflows/definitions?tab=active" },
            { "workflows/instances", "workflows/instances" }
        };

        cases.Add(EncodeTimes("//evil.com", 9), "/");
        cases.Add(EncodeTimes("//", 9), "/");
        cases.Add("/" + EncodeTimes("//evil.com", 9), "/");

        return cases;
    }
}
