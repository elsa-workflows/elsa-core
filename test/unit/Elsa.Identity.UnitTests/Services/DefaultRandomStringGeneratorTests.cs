using Elsa.Identity.Services;

namespace Elsa.Identity.UnitTests.Services;

public class DefaultRandomStringGeneratorTests
{
    [Fact]
    public void Generate_ReturnsRequestedLengthFromAllowedCharacters()
    {
        var generator = new DefaultRandomStringGenerator();

        var value = generator.Generate(64, ['a', 'b']);

        Assert.Equal(64, value.Length);
        Assert.All(value, x => Assert.True(x is 'a' or 'b'));
    }

    [Fact]
    public void ApiKeyGenerator_UsesHighEntropyRandomSuffix()
    {
        var generator = new DefaultApiKeyGeneratorAndParser();

        var apiKey = generator.Generate("client-1");
        var suffix = apiKey.Split('-', 2)[1];
        var bytes = Convert.FromHexString(suffix);

        Assert.Equal(64, suffix.Length);
        Assert.Equal(32, bytes.Length);
    }
}
