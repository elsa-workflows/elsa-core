namespace Elsa.Secrets.Contracts;

public interface ISecretValueProtector
{
    string Protect(string value);
    string Unprotect(string protectedValue);
}
