namespace Elsa.Secrets.Types;

public class TextSecretTypeProvider : ISecretTypeProvider
{
    public SecretTypeDescriptor Descriptor { get; } = new(
        SecretTypeNames.Text,
        "Text",
        "A text value such as a password, token, or connection string.",
        "secret-text",
        [SecretStoreNames.Encrypted, SecretStoreNames.Configuration]);

    public bool Validate(CreateSecretRequest request, out string? error)
    {
        if (request.StoreName == SecretStoreNames.Encrypted && string.IsNullOrEmpty(request.Value))
        {
            error = "A text value is required for encrypted secrets.";
            return false;
        }

        if (request.StoreName == SecretStoreNames.Configuration && string.IsNullOrWhiteSpace(request.ConfigurationKey))
        {
            error = "A configuration key is required for configuration-backed secrets.";
            return false;
        }

        error = null;
        return true;
    }

    public bool ValidateRotation(RotateSecretRequest request, string storeName, out string? error)
    {
        if (storeName == SecretStoreNames.Encrypted && string.IsNullOrEmpty(request.Value))
        {
            error = "A replacement value is required.";
            return false;
        }

        if (storeName == SecretStoreNames.Configuration && string.IsNullOrWhiteSpace(request.ConfigurationKey))
        {
            error = "A replacement configuration key is required.";
            return false;
        }

        error = null;
        return true;
    }
}
