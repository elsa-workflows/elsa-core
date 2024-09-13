using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Secrets.Extensions;
using Elsa.Secrets.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Secrets.Management.Features;

[DependsOn(typeof(SecretsFeature))]
public class SecretManagementFeature(IModule module) : FeatureBase(module)
{
    private Func<IServiceProvider, ISecretStore> _secretStoreFactory = sp => sp.GetRequiredService<MemorySecretStore>();
    private Func<IServiceProvider, IDecryptor> _decryptorFactory = sp => sp.GetRequiredService<DataProtectionEncryptor>();
    private Func<IServiceProvider, IEncryptor> _encryptorFactory = sp => sp.GetRequiredService<DataProtectionEncryptor>();

    public SecretManagementFeature UseSecretsStore(Func<IServiceProvider, ISecretStore> secretStoreFactory)
    {
        _secretStoreFactory = secretStoreFactory;
        return this;
    }

    public override void Configure()
    {
        Module.UseSecrets(secrets =>
        {
            secrets.UseSecretsProvider(sp => sp.GetRequiredService<StoreSecretProvider>());
        });
    }

    public override void Apply()
    {
        Services
            .AddScoped(_secretStoreFactory)
            .AddScoped(_decryptorFactory)
            .AddScoped(_encryptorFactory)
            .AddScoped<DataProtectionEncryptor>()
            .AddScoped<StoreSecretProvider>()
            .AddMemoryStore<Secret, MemorySecretStore>()
            .AddScoped<ISecretManager, DefaultSecretManager>()
            ;
    }
}