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
        var bytes = Convert.FromHexString(suffix);

        Assert.Equal(64, suffix.Length);
        Assert.Equal(32, bytes.Length);
    }
}
