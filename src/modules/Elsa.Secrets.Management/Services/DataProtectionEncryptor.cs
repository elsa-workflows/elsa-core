using Microsoft.AspNetCore.DataProtection;

namespace Elsa.Secrets.Management;

/// <inheritdoc cref="Elsa.Secrets.Management.IEncryptor" />
/// <inheritdoc cref="Elsa.Secrets.Management.IDecryptor" />
public class DataProtectionEncryptor(IDataProtectionProvider dataProtectionProvider) : IEncryptor, IDecryptor
{
    /// <inheritdoc />
    public Task<string> EncryptAsync(string value, CancellationToken cancellationToken = default)
    {
        if(string.IsNullOrWhiteSpace(value))
            return Task.FromResult(value);
        
        var protector = GetDataProtector();
        return Task.FromResult(protector.Protect(value));
    }

    /// <inheritdoc />
    public Task<string> DecryptAsync(string encryptedValue, CancellationToken cancellationToken = default)
    {
        if(string.IsNullOrWhiteSpace(encryptedValue))
            return Task.FromResult(encryptedValue);
        
        var protector = GetDataProtector();
        var decryptedValue = protector.Unprotect(encryptedValue);
        return Task.FromResult(decryptedValue);
    }

    private IDataProtector GetDataProtector()
    {
        return dataProtectionProvider.CreateProtector("Elsa.Secrets.Encryption");
    }
}