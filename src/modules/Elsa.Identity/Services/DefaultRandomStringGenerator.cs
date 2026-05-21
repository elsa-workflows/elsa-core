using System.Security.Cryptography;
using Elsa.Identity.Constants;
using Elsa.Identity.Contracts;

namespace Elsa.Identity.Services;

/// <inheritdoc />
public class DefaultRandomStringGenerator : IRandomStringGenerator
{
    /// <inheritdoc />
    public string Generate(int length = 32, char[]? chars = null)
    {
        chars ??= CharacterSequences.AlphanumericSequence;
        return RandomNumberGenerator.GetString(chars, length);
    }
}
