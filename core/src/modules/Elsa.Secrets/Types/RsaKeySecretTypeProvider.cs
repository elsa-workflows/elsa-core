namespace Elsa.Secrets.Types;

public class RsaKeySecretTypeProvider : ISecretTypeProvider
{
    public SecretTypeDescriptor Descriptor { get; } = new(
        SecretTypeNames.RsaKey,
        "RSA Key",
        "RSA key material stored as encrypted text or referenced from configuration.",
        "secret-rsa-key",
        [SecretStoreNames.Encrypted, SecretStoreNames.Configuration]);

    public bool Validate(CreateSecretRequest request, out string? error) => ValidatePayload(request.StoreName, request.Value, request.ConfigurationKey, out error);

    public bool ValidateRotation(RotateSecretRequest request, string storeName, out string? error) => ValidatePayload(storeName, request.Value, request.ConfigurationKey, out error);

    private static bool ValidatePayload(string storeName, string? value, string? configurationKey, out string? error)
    {
        if (storeName == SecretStoreNames.Encrypted && string.IsNullOrWhiteSpace(value))
        {
            error = "RSA key material is required for encrypted secrets.";
            return false;
        }

        if (storeName == SecretStoreNames.Configuration && string.IsNullOrWhiteSpace(configurationKey))
        {
            error = "A configuration key is required for configuration-backed RSA key secrets.";
            return false;
        }

        error = null;
        return true;
    }
}
