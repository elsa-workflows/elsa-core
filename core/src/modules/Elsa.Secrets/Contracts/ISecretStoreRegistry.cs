namespace Elsa.Secrets.Contracts;

public interface ISecretStoreRegistry
{
    IReadOnlyCollection<ISecretStore> List();
    ISecretStore Get(string name);
    bool TryGet(string name, out ISecretStore? store);
}
