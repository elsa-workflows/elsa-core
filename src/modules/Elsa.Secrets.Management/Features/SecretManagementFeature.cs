using Elsa.Common.RecurringTasks;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Secrets.Extensions;
using Elsa.Secrets.Features;
using Elsa.Secrets.Management.Tasks;
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

    public SecretManagementFeature ConfigureOptions(Action<SecretManagementOptions> configureOptions)
    {
        Services.Configure(configureOptions);
        return this;
    }

    public override void Configure()
    {
        Module.UseSecrets(secrets =>
        {
            secrets.UseSecretsProvider(sp => sp.GetRequiredService<StoreSecretProvider>());
        });

        Services.Configure<RecurringTaskOptions>(options =>
        {
            options.Schedule.ConfigureTask<UpdateExpiredSecretsRecurringTask>(TimeSpan.FromHours(4));
        });
    }

    public override void Apply()
    {
        Services
            .AddScoped(_secretStoreFactory)
            .AddScoped(_decryptorFactory)
            .AddScoped(_encryptorFactory)
            .AddScoped<ISecretEncryptor, DefaultSecretEncryptor>()
            .AddScoped<DataProtectionEncryptor>()
            .AddScoped<StoreSecretProvider>()
            .AddMemoryStore<Secret, MemorySecretStore>()
            .AddScoped<ISecretNameGenerator, DefaultSecretNameGenerator>()
            .AddScoped<ISecretNameValidator, DefaultSecretNameValidator>()
            .AddScoped<ISecretUpdater, DefaultSecretUpdater>()
            .AddScoped<ISecretManager, DefaultSecretManager>()
            .AddScoped<IExpiredSecretsUpdater, DefaultExpiredSecretsUpdater>()
            .AddRecurringTask<UpdateExpiredSecretsRecurringTask>()
            ;
    }
}