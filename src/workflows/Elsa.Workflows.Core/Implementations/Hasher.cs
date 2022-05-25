using System.Security.Cryptography;
using System.Text;
using Elsa.Services;

namespace Elsa.Implementations;

public class Hasher : IHasher
{
    private readonly IBookmarkDataSerializer _bookmarkDataSerializer;

    public Hasher(IBookmarkDataSerializer bookmarkDataSerializer)
    {
        _bookmarkDataSerializer = bookmarkDataSerializer;
    }
    
    public string Hash(object value)
    {
        var json = _bookmarkDataSerializer.Serialize(value);
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