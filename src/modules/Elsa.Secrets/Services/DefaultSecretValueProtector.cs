using System.Security.Cryptography;
using Microsoft.Extensions.Options;

namespace Elsa.Secrets.Services;

public class DefaultSecretValueProtector(IOptions<SecretsOptions> options) : ISecretValueProtector
{
    private const int NonceSize = 12;
    private const int TagSize = 16;

    public string Protect(string value)
    {
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var plaintext = System.Text.Encoding.UTF8.GetBytes(value);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(GetKey(), TagSize);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        return string.Join(".", "v1", Convert.ToBase64String(nonce), Convert.ToBase64String(tag), Convert.ToBase64String(ciphertext));
    }

    public string Unprotect(string protectedValue)
    {
        var parts = protectedValue.Split('.');
        if (parts.Length != 4 || parts[0] != "v1")
            throw new InvalidOperationException("The protected secret payload is not supported.");

        var nonce = Convert.FromBase64String(parts[1]);
        var tag = Convert.FromBase64String(parts[2]);
        var ciphertext = Convert.FromBase64String(parts[3]);
        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(GetKey(), TagSize);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);

        return System.Text.Encoding.UTF8.GetString(plaintext);
    }

    private byte[] GetKey()
    {
        var key = options.Value.EncryptionKey;
        if (key == null || key.Length == 0)
            throw new InvalidOperationException("Elsa Secrets encryption key is not configured. Configure SecretsOptions.EncryptionKey before using the encrypted secrets store.");

        if (key.Length is not (16 or 24 or 32))
            throw new InvalidOperationException("Elsa Secrets encryption key must be exactly 16, 24, or 32 bytes.");

        return key;
    }
}
