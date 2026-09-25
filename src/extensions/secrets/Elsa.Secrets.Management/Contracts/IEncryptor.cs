namespace Elsa.Secrets.Management;

/// <summary>
/// Encrypts a value.
/// </summary>
public interface IEncryptor
{
    Task<string> EncryptAsync(string value, CancellationToken cancellationToken = default);
}