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

}
