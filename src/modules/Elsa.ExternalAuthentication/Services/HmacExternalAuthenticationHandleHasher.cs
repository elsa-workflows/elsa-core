using System.Security.Cryptography;
using System.Text;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Options;
using Microsoft.Extensions.Options;

namespace Elsa.ExternalAuthentication.Services;

/// <summary>
/// HMAC-SHA-256 handle hasher. It uses a configured shared key when present and a process-local
/// key for single-node development otherwise.
/// </summary>
public sealed class HmacExternalAuthenticationHandleHasher : IExternalAuthenticationHandleHasher, IDisposable
{
    private readonly byte[] _key;

    /// <summary>
    /// Creates a process-local hasher for tests and single-node development.
    /// </summary>
    public HmacExternalAuthenticationHandleHasher() : this(RandomNumberGenerator.GetBytes(32))
    {
    }

    /// <summary>
    /// Creates a hasher from deployment-owned External Authentication options.
    /// </summary>
    public HmacExternalAuthenticationHandleHasher(IOptions<ExternalAuthenticationOptions> options)
        : this(GetKey(options.Value.HandleHashing))
    {
    }

    private HmacExternalAuthenticationHandleHasher(byte[] key)
    {
        _key = key;
    }

    public string Hash(string value) => Convert.ToHexString(HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes(value)));

    public void Dispose() => CryptographicOperations.ZeroMemory(_key);

    private static byte[] GetKey(ExternalAuthenticationHandleHashingOptions? options)
    {
        if (options is null)
            throw new InvalidOperationException("External Authentication handle-hashing settings are required.");

        if (string.IsNullOrWhiteSpace(options.SharedKeyBase64))
            return RandomNumberGenerator.GetBytes(32);

        try
        {
            var key = Convert.FromBase64String(options.SharedKeyBase64);
            if (key.Length >= 32)
                return key;

            CryptographicOperations.ZeroMemory(key);
        }
        catch (FormatException)
        {
            // The options validator reports the actionable configuration error at startup.
        }

        throw new InvalidOperationException("The External Authentication shared handle-hashing key must be valid base64 containing at least 32 bytes.");
    }
}
