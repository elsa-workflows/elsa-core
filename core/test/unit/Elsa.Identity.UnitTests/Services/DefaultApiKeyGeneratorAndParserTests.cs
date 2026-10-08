using Elsa.Identity.Services;

namespace Elsa.Identity.UnitTests.Services;

public class DefaultApiKeyGeneratorAndParserTests
{
    [Fact]
    public void Generate_UsesHighEntropyRandomSuffix()
    {
        var generator = new DefaultApiKeyGeneratorAndParser();

        var apiKey = generator.Generate("client-1");
        var suffix = apiKey.Split('-', 2)[1];

        Assert.Equal(36, suffix.Length);
        Assert.True(Guid.TryParseExact(suffix, "D", out _));
    }
}
