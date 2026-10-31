namespace Elsa.Secrets.Contracts;

public interface ISecretTypeRegistry
{
    IReadOnlyCollection<SecretTypeDescriptor> List();
    ISecretTypeProvider Get(string name);
    bool TryGet(string name, out ISecretTypeProvider? provider);
}
