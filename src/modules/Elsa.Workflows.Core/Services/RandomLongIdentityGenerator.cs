using System.Security.Cryptography;
using Elsa.Workflows.Contracts;

namespace Elsa.Workflows.Services;

/// <summary>
/// Generates a unique identifier using a random long value.
/// </summary>
public class RandomLongIdentityGenerator : IIdentityGenerator
{
    /// <inheritdoc />
    public string GenerateId()
    {
        var bytes = new byte[8];
        RandomNumberGenerator.Fill(bytes);
        var randomLong = BitConverter.ToInt64(bytes, 0);
        return randomLong.ToString("x");
    }
}