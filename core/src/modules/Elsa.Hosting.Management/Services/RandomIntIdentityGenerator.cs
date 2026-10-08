using System.Security.Cryptography;
using Elsa.Workflows;

namespace Elsa.Hosting.Management.Services;

/// <summary>
/// Generates a unique identifier using a random short value.
/// </summary>
public class RandomIntIdentityGenerator : IIdentityGenerator
{
    /// <inheritdoc />
    public string GenerateId()
    {
        var bytes = new byte[4];
        RandomNumberGenerator.Fill(bytes);
        var randomShort = BitConverter.ToInt32(bytes, 0);
        return randomShort.ToString("x");
    }
}