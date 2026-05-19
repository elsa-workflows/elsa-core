using Elsa.Secrets.Contracts;
using Elsa.Secrets.Models;
using Elsa.Secrets.Options;
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
        var options = Microsoft.Extensions.Options.Options.Create(new SecretsOptions());
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
        Manager = new DefaultSecretManager(new DefaultSecretNameValidator(), StoreRegistry, TypeRegistry);
        Resolver = new DefaultSecretResolver(Manager);
    }

    public DefaultSecretManager Manager { get; }
    public ISecretResolver Resolver { get; }
    public ISecretStoreRegistry StoreRegistry { get; }
    public ISecretTypeRegistry TypeRegistry { get; }
}
