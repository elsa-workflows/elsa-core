using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Secrets.Extensions;
using Elsa.Secrets.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Secrets.Management.Features;

[DependsOn(typeof(SecretsFeature))]
public class SecretsManagementFeature(IModule module) : FeatureBase(module)
{
    private Func<IServiceProvider, ISecretStore> _secretStoreFactory = sp => sp.GetRequiredService<MemorySecretStore>();
    private Func<IServiceProvider, IAlgorithmResolver> _algorithmResolver = sp => sp.GetRequiredService<DefaultAlgorithmResolver>();
    private Func<IServiceProvider, IDecryptor> _decryptorFactory = sp => sp.GetRequiredService<DefaultEncryptor>();
    private Func<IServiceProvider, IEncryptor> _encryptorFactory = sp => sp.GetRequiredService<DefaultEncryptor>();
    private Func<IServiceProvider, IEncryptionKeyProvider> _encryptionKeyProvider = sp => sp.GetRequiredService<OptionsEncryptionKeyProvider>();

    public SecretsManagementFeature UseOptionsEncryptionKeyProvider()
    {
        _encryptionKeyProvider = sp => sp.GetRequiredService<OptionsEncryptionKeyProvider>();
        return this;
    }

    public SecretsManagementFeature UseStoreEncryptionKeyProvider()
    {
        _encryptionKeyProvider = sp => sp.GetRequiredService<StoreEncryptionKeyProvider>();
        return this;
    }

    public SecretsManagementFeature UseSecretsStore(Func<IServiceProvider, ISecretStore> secretStoreFactory)
    {
        _secretStoreFactory = secretStoreFactory;
        return this;
    }

    public SecretsManagementFeature UseAlgorithmResolver(Func<IServiceProvider, IAlgorithmResolver> algorithmResolverFactory)
    {
        _algorithmResolver = algorithmResolverFactory;
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
            .AddScoped(_algorithmResolver)
            .AddScoped(_decryptorFactory)
            .AddScoped(_encryptorFactory)
            .AddScoped(_encryptionKeyProvider)
            .AddScoped<DefaultEncryptor>()
            .AddScoped<StoreSecretProvider>()
            .AddTransient<IAlgorithmProvider, DefaultAlgorithmProvider>()
            ;
    }
}