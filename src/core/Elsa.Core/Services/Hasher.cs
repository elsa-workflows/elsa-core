using System.Security.Cryptography;
using System.Text;
using Elsa.Contracts;

namespace Elsa.Services;

public class Hasher : IHasher
{
    private readonly IPayloadSerializer _payloadSerializer;

    public Hasher(IPayloadSerializer payloadSerializer)
    {
        _payloadSerializer = payloadSerializer;
    }
    
    public string Hash(object value)
    {
        var json = _payloadSerializer.Serialize(value);
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