namespace Elsa.Secrets.Types;

public class X509CertificateSecretTypeProvider : ISecretTypeProvider
{
    public SecretTypeDescriptor Descriptor { get; } = new(
        SecretTypeNames.X509Certificate,
        "X.509 Certificate",
        "A certificate reference, such as a thumbprint or configuration-backed certificate identity.",
        "secret-x509-certificate",
        [SecretStoreNames.Encrypted, SecretStoreNames.Configuration]);

    public bool Validate(CreateSecretRequest request, out string? error) => ValidatePayload(request.StoreName, request.Value, request.ConfigurationKey, request.Metadata, out error);

    public bool ValidateRotation(RotateSecretRequest request, string storeName, out string? error) => ValidatePayload(storeName, request.Value, request.ConfigurationKey, request.Metadata, out error);

    private static bool ValidatePayload(string storeName, string? value, string? configurationKey, IDictionary<string, string> metadata, out string? error)
    {
        var hasThumbprint = metadata.TryGetValue("thumbprint", out var thumbprint) && !string.IsNullOrWhiteSpace(thumbprint);
        if (storeName == SecretStoreNames.Encrypted && string.IsNullOrWhiteSpace(value) && !hasThumbprint)
        {
            error = "Certificate material or a thumbprint metadata value is required.";
            return false;
        }

        if (storeName == SecretStoreNames.Configuration && string.IsNullOrWhiteSpace(configurationKey))
        {
            error = "A configuration key is required for configuration-backed certificate secrets.";
            return false;
        }

        error = null;
        return true;
    }
}
