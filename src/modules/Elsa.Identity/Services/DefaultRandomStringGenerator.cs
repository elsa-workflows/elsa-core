using System.Text;
using Elsa.Identity.Constants;
using Elsa.Identity.Contracts;

namespace Elsa.Identity.Services;

/// <inheritdoc />
public class DefaultRandomStringGenerator : IRandomStringGenerator
{
    private readonly Random _random;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultRandomStringGenerator"/> class.
    /// </summary>
    public DefaultRandomStringGenerator()
    {
        _random = new Random();
    }

    /// <inheritdoc />
    public string Generate(int length = 32, char[]? chars = null)
    {
        var identifierBuilder = new StringBuilder(length);
        
        chars ??= CharacterSequences.AlphanumericSequence;

        for (var i = 0; i < length; i++)
        {
            var randomIndex = _random.Next(chars.Length);
            identifierBuilder.Append(chars[randomIndex]);
        }

        return identifierBuilder.ToString();
    }
}