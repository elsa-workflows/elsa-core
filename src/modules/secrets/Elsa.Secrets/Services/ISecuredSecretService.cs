using System.Collections.Generic;
using Elsa.Expressions;
using Elsa.Secrets.Models;

namespace Elsa.Secrets.Services;

public interface ISecuredSecretService
{
    string? GetProperty(string name, string syntax = SyntaxNames.Literal);
    IEnumerable<SecretProperty> GetAllProperties();
    void EncryptProperties();
    void AddOrUpdateProperty(string name, string value, string syntax = SyntaxNames.Literal);
    void RemoveProperty(string name);
    void SetSecret(Secret secret);
}