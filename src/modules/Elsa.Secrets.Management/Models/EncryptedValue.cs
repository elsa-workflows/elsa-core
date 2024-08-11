namespace Elsa.Secrets.Management;

public record EncryptedValue(string CipherText, string IV, string KeyId);