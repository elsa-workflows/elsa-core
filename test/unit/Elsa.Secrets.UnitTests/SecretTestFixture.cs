using Elsa.Secrets.Contracts;
using Elsa.Secrets.Models;
using Elsa.Secrets.Options;
using Elsa.Secrets.Repositories;
using Elsa.Secrets.Services;
using Elsa.Secrets.Stores;
using Elsa.Secrets.Types;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Elsa.Secrets.UnitTests;

public class SecretTestFixture
{
    public SecretTestFixture(IConfiguration? configuration = null)
    {
        var options = Microsoft.Extensions.Options.Options.Create(new SecretsOptions { EncryptionKey = "0123456789abcdef0123456789abcdef"u8.ToArray() });
        var protector = new DefaultSecretValueProtector(options);
        var stores = new ISecretStore[]
        {
            new EncryptedSecretStore(protector),
            new ConfigurationSecretStore(configuration ?? new ConfigurationBuilder().Build(), options)
        };
        var types = new ISecretTypeProvider[]
        {
            new TextSecretTypeProvider(),
            new RsaKeySecretTypeProvider(),
            new X509CertificateSecretTypeProvider()
        };

        StoreRegistry = new SecretStoreRegistry(stores);
        TypeRegistry = new SecretTypeRegistry(types);
        Repository = new InMemorySecretRepository();
        Manager = new DefaultSecretManager(new DefaultSecretNameValidator(), StoreRegistry, TypeRegistry, Repository);
        Resolver = new DefaultSecretResolver(Manager);
    }

    public DefaultSecretManager Manager { get; }
    public ISecretRepository Repository { get; }
    public ISecretResolver Resolver { get; }
    public ISecretStoreRegistry StoreRegistry { get; }
    public ISecretTypeRegistry TypeRegistry { get; }
}
