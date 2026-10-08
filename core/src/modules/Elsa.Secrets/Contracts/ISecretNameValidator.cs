namespace Elsa.Secrets.Contracts;

public interface ISecretNameValidator
{
    bool IsValid(string? name, out string? error);
    string Normalize(string name);
}
