using Elsa.Identity.Services;
using System.Threading.Tasks;

namespace Elsa.Identity.UnitTests.Services;

public class DefaultRandomStringGeneratorTests
{
    [Test]
    public async Task Generate_ReturnsRequestedLengthFromAllowedCharacters()
    {
        var generator = new DefaultRandomStringGenerator();

        var value = generator.Generate(64, new[] { 'a', 'b' });

        await Assert.That(value.Length).IsEqualTo(64);
        await Assert.That(value.All(x => x is 'a' or 'b')).IsTrue();
    }

}