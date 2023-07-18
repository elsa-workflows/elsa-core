using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.Encryption;
using Elsa.Expressions;
using Elsa.Secrets.Models;
using Microsoft.Extensions.Configuration;

namespace Elsa.Secrets.Services;

public class SecuredSecretService : ISecuredSecretService
{
    private readonly string _encryptionKey;
    private readonly string[] _encryptedProperties;
    private Secret _secret;

    public SecuredSecretService(IConfiguration configuration)
    {
        var section = configuration.GetSection("Elsa:Features:Secrets");
        _encryptionKey = section.GetValue<string>("EncryptionKey");
        _encryptedProperties = section.GetSection("EncryptedProperties")
            .AsEnumerable()
            .Select(x => x.Value)
            .Where(x => !string.IsNullOrEmpty(x))
            .ToArray();
    }

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
        if (property.IsEncrypted)
        {
            value = AesEncryption.Decrypt(_encryptionKey, value);
        }

        return value;
    }

    public IEnumerable<SecretProperty> GetAllProperties()
    {
        if (_secret == null)
            throw new Exception("Secret has to be set before calling any methods");

        return _secret.Properties
            .Select(property =>
            {
                if (!property.IsEncrypted)
                    return property;

                var decryptedProperty = new SecretProperty(property.Name, new Dictionary<string, string?>(), property.Syntax);
                foreach (var key in property.Expressions.Keys)
                {
                    var value = property.Expressions[key];
                    var decrypted = AesEncryption.Decrypt(_encryptionKey, value);
                    decryptedProperty.Expressions.Add(key, decrypted);
                }

                return decryptedProperty;
            });
    }

    public void EncryptProperties()
    {
        if (_secret == null)
            throw new Exception("Secret has to be set before calling any methods");

        foreach (var property in _secret.Properties)
        {
            var encrypt = _encryptedProperties.Contains(property.Name, StringComparer.OrdinalIgnoreCase);
            if (!encrypt || property.IsEncrypted)
            {
                continue;
            }

            foreach (var key in property.Expressions.Keys)
            {
                var value = property.Expressions[key];
                property.Expressions[key] = AesEncryption.Encrypt(_encryptionKey, value);
            }

            property.IsEncrypted = true;
        }
    }

    public void AddOrUpdateProperty(string name, string value, string syntax = SyntaxNames.Literal)
    {
        if (_secret == null)
            throw new Exception("Secret has to be set before calling any methods");

        RemoveProperty(name);

        var encrypt = _encryptedProperties.Contains(name, StringComparer.OrdinalIgnoreCase);
        if (encrypt)
        {
            value = AesEncryption.Encrypt(_encryptionKey, value);
        }

        var property = new SecretProperty(name, new Dictionary<string, string?> { [syntax] = value }, syntax);
        property.IsEncrypted = encrypt;
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