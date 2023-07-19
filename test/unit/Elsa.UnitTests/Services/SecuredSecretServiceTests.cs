using System;
using System.Collections.Generic;
using System.Linq;
using AutoFixture.Xunit2;
using Elsa.Secrets.Models;
using Elsa.Secrets.Services;
using Elsa.Testing.Shared;
using Elsa.Testing.Shared.AutoFixture.Attributes;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Elsa.Services;

public class SecuredSecretServiceTests
{
    private IConfiguration GetConfiguration()
    {
        var inMemorySettings = new Dictionary<string, string> {
            {"Elsa:Features:Secrets:EncryptionKey", "ogZkAw03V0l1hlLcwy3QTrKcmvzoErDN"},
            {"Elsa:Features:Secrets:EncryptedProperties:0", "clientsecret" },
            {"Elsa:Features:Secrets:EncryptedProperties:1", "clientid" },
            {"Elsa:Features:Secrets:EncryptedProperties:2", "user id" },
            {"Elsa:Features:Secrets:EncryptedProperties:3", "password" },
            {"Elsa:Features:Secrets:EncryptedProperties:4", "user" }
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
    }
    [Theory(DisplayName = "SecuredSecretService encrypts only properties defined in configuration"), AutoMoqData]
    public void SecuredSecretServiceEncryptsPropertiesDefinedInConfiguration()
    {
        var configuration = GetConfiguration();
        
        var securedSecretService = new SecuredSecretService(configuration);

        var secret = new Secret()
        {
            Type = "Authorization",
            Name = "Test",
            Properties = new List<SecretProperty>
            {
                SecretProperty.Literal("url", "abc.test.com"),
                SecretProperty.Literal("clientid", "test-client-id"),
                SecretProperty.Literal("clientsecret", "test-client-secret"),
                SecretProperty.Literal("scope", "randomscope"),
            }
        };
        securedSecretService.SetSecret(secret);
        securedSecretService.EncryptProperties();
        
        // Check undecrypted url
        var savedUrl = secret.Properties.FirstOrDefault(x => x.Name == "url")?.GetExpression("Literal");
        Assert.Equal(savedUrl, "abc.test.com");

        // Check undecrypted client id
        var savedClientId = secret.Properties.FirstOrDefault(x => x.Name == "clientid")?.GetExpression("Literal");
        Assert.NotEqual(savedClientId, "test-client-id");
        
        // Check decrypted client id
        Assert.Equal(securedSecretService.GetProperty("clientid"), "test-client-id");
        
        // Check undecrypted client secret
        var savedClientSecret = secret.Properties.FirstOrDefault(x => x.Name == "clientsecret")?.GetExpression("Literal");
        Assert.NotEqual(savedClientSecret, "test-client-secret");
        
        // Check decrypted client secret
        Assert.Equal(securedSecretService.GetProperty("clientsecret"), "test-client-secret");
        
        // Check undecrypted scope
        var savedScope = secret.Properties.FirstOrDefault(x => x.Name == "scope")?.GetExpression("Literal");
        Assert.Equal(savedScope, "randomscope");
    }
}