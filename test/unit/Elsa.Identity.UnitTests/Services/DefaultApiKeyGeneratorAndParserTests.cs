using Elsa.Identity.Services;
using System.Threading.Tasks;

namespace Elsa.Identity.UnitTests.Services;

public class DefaultApiKeyGeneratorAndParserTests
{
    [Test]
    public async Task Generate_UsesHighEntropyRandomSuffix()
    {
        var generator = new DefaultApiKeyGeneratorAndParser();

        var apiKey = generator.Generate("client-1");
        var suffix = apiKey.Split('-', 2)[1];

        await Assert.That(suffix.Length).IsEqualTo(36);
        await Assert.That(Guid.TryParseExact(suffix, "D", out _)).IsTrue();
    }
}