using System.Security.Cryptography;
using System.Text;
using Elsa.Identity.Constants;
using Elsa.Identity.Contracts;

namespace Elsa.Identity.Services;

/// <inheritdoc />
public class DefaultRandomStringGenerator : IRandomStringGenerator
{
    /// <inheritdoc />
    public string Generate(int length = 32, char[]? chars = null)
    {
        var identifierBuilder = new StringBuilder(length);
        
        chars ??= CharacterSequences.AlphanumericSequence;

        for (var i = 0; i < length; i++)
        {
            var randomIndex = RandomNumberGenerator.GetInt32(chars.Length);
            identifierBuilder.Append(chars[randomIndex]);
        }

        return identifierBuilder.ToString();
    }
}
