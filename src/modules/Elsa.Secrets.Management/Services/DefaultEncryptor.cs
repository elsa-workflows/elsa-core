using Elsa.Extensions;

namespace Elsa.Secrets.Management;

/// <inheritdoc cref="Elsa.Secrets.Management.IEncryptor" />
/// <inheritdoc cref="Elsa.Secrets.Management.IDecryptor" />
public class DefaultEncryptor(IEncryptionKeyProvider encryptionKeyProvider, IAlgorithmResolver algorithmResolver) : IEncryptor, IDecryptor
{
    /// <inheritdoc />
    public async Task<EncryptedValue> EncryptAsync(string keyId, string value, CancellationToken cancellationToken = default)
    {
        var encryptionKey = await encryptionKeyProvider.GetAsync(keyId, cancellationToken);
        var algorithmName = encryptionKey.Algorithm;
        var key = encryptionKey.Key;
        var algorithm = await algorithmResolver.ResolveAsync(algorithmName, cancellationToken);
        var iv = Convert.ToBase64String(algorithm.WithGeneratedIV().IV);
        var encryptedValue = algorithm.Encrypt(value, key, iv);
        return new EncryptedValue(encryptedValue, iv, encryptionKey.Id);
    }

    /// <inheritdoc />
    public async Task<string> DecryptAsync(EncryptedValue encryptedValue, CancellationToken cancellationToken = default)
    {
        var encryptionKey = await encryptionKeyProvider.GetAsync(encryptedValue.KeyId, cancellationToken);
        var algorithm = await algorithmResolver.ResolveAsync(encryptionKey.Algorithm, cancellationToken);
        var decryptedValue = algorithm.Decrypt(encryptedValue.CipherText, encryptionKey.Key, encryptedValue.IV);
        return decryptedValue;
    }
}