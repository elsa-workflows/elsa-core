using System.Security.Cryptography;
using System.Text;
using Elsa.Workflows.Core.Contracts;

namespace Elsa.Workflows.Core.Services;

/// <inheritdoc />
public class Hasher : IHasher
{
    /// <inheritdoc />
    public string Hash(string value)
    {
        using var sha = SHA256.Create();
        return Hash(sha, value);
    }
    
    private static string Hash(HashAlgorithm hashAlgorithm, string input)
    {
        var data = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(data);
    }
}