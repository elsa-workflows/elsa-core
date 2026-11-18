using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace Elsa.Secrets.Services;

public class DefaultSecretValueProtector(IOptions<SecretsOptions> options) : ISecretValueProtector
{
    private const int NonceSize = 12;
    private const int TagSize = 16;

    public string Protect(string value)
    {
        var plaintext = Encoding.UTF8.GetBytes(value);
        try
        {
            return Protect(plaintext, GetKey());
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }

    public string Unprotect(string protectedValue)
    {
        var decrypted = Decrypt(protectedValue);
        try
        {
            return Encoding.UTF8.GetString(decrypted.Bytes);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(decrypted.Bytes);
        }
    }

    internal string Reencrypt(string protectedValue)
    {
        var decrypted = Decrypt(protectedValue);
        try
        {
            return decrypted.UsedCurrentKey ? protectedValue : Protect(decrypted.Bytes, GetKey());
        }
        finally
        {
            CryptographicOperations.ZeroMemory(decrypted.Bytes);
        }
    }

    private static string Protect(byte[] plaintext, byte[] key)
    {
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);
        return string.Join(".", "v1", Convert.ToBase64String(nonce), Convert.ToBase64String(tag), Convert.ToBase64String(ciphertext));
    }

    private (byte[] Bytes, bool UsedCurrentKey) Decrypt(string protectedValue)
    {
        var parts = protectedValue.Split('.');
        if (parts.Length != 4 || parts[0] != "v1")
            throw new InvalidOperationException("The protected secret payload is not supported.");

        var nonce = Convert.FromBase64String(parts[1]);
        var tag = Convert.FromBase64String(parts[2]);
        var ciphertext = Convert.FromBase64String(parts[3]);
        var plaintext = new byte[ciphertext.Length];

        try
        {
            if (TryDecrypt(GetKey()))
                return (plaintext, true);

            foreach (var key in options.Value.PreviousEncryptionKeys)
                if (key is { Length: 16 or 24 or 32 } && TryDecrypt(key))
                    return (plaintext, false);

            throw new AuthenticationTagMismatchException("The protected secret payload does not match any configured key.");
        }
        catch
        {
            CryptographicOperations.ZeroMemory(plaintext);
            throw;
        }

        bool TryDecrypt(byte[] key)
        {
            try
            {
                using var aes = new AesGcm(key, TagSize);
                aes.Decrypt(nonce, ciphertext, tag, plaintext);
                return true;
            }
            catch (AuthenticationTagMismatchException)
            {
                return false;
            }
        }
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
