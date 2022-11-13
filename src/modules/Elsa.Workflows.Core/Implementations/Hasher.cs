using System.Security.Cryptography;
using System.Text;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Implementations;

public class Hasher : IHasher
{
    private readonly IBookmarkPayloadSerializer _bookmarkPayloadSerializer;

    public Hasher(IBookmarkPayloadSerializer bookmarkPayloadSerializer)
    {
        _bookmarkPayloadSerializer = bookmarkPayloadSerializer;
    }
    
    public string Hash(object value)
    {
        var json = _bookmarkPayloadSerializer.Serialize(value);
        return Hash(json);
    }
    
    public string Hash(string value)
    {
        using var sha = HashAlgorithm.Create(HashAlgorithmName.SHA256.ToString())!;
        return Hash(sha, value);
    }
    
    private static string Hash(HashAlgorithm hashAlgorithm, string input)
    {
        var data = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(data);
    }
}