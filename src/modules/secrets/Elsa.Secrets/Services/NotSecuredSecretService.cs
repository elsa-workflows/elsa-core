using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.Expressions;
using Elsa.Secrets.Models;

namespace Elsa.Secrets.Services;

public class NotSecuredSecretService : ISecuredSecretService
{
    private Secret _secret;

    public void SetSecret(Secret secret)
    {
        _secret = secret;
    }

    public string? GetProperty(string name, string syntax = SyntaxNames.Literal)
    {
        if (_secret == null)
            throw new Exception("Secret has to be set before calling any methods");

        var property = _secret.Properties?.FirstOrDefault(r => r.Name == name);
        if (property == null)
        {
            return null;
        }

        var value = property.GetExpression(syntax);
        return value;
    }

    public IEnumerable<SecretProperty> GetAllProperties()
    {
        if (_secret == null)
            throw new Exception("Secret has to be set before calling any methods");

        return _secret.Properties;
    }

    public void EncryptProperties()
    {
    }

    public void AddOrUpdateProperty(string name, string value, string syntax = SyntaxNames.Literal)
    {
        if (_secret == null)
            throw new Exception("Secret has to be set before calling any methods");

        RemoveProperty(name);

        var property = new SecretProperty(name, new Dictionary<string, string?> { [syntax] = value }, syntax);
        _secret.Properties.Add(property);
    }
    
    public void RemoveProperty(string name)
    {
        var property = _secret.Properties.FirstOrDefault(x => x.Name == name);
        if (property != null)
        {
            _secret.Properties.Remove(property);
        }
    }
}