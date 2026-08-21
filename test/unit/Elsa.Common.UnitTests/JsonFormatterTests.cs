using Elsa.Common.Services;

namespace Elsa.Common.UnitTests;

public class JsonFormatterTests
{
    [Fact]
    public async Task ToStringAsync_DoesNotEscapeUnicodeCharacters()
    {
        var formatter = new JsonFormatter();
        var value = new { Name = "ÆØÅ" };

        var json = await formatter.ToStringAsync(value);

        Assert.Contains("ÆØÅ", json);
        Assert.DoesNotContain(@"\u00C6", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(@"\u00D8", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(@"\u00C5", json, StringComparison.OrdinalIgnoreCase);
    }
}
