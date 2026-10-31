namespace Elsa.Secrets.Contracts;

public interface ISecretTypeProvider
{
    SecretTypeDescriptor Descriptor { get; }
    bool Validate(CreateSecretRequest request, out string? error);
    bool ValidateRotation(RotateSecretRequest request, string storeName, out string? error);
}
