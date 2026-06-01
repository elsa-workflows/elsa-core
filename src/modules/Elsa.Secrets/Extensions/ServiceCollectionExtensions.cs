using Elsa.Expressions.Contracts;
using Elsa.Secrets.Providers;
using Elsa.Secrets.Repositories;
using Elsa.Secrets.Services;
using Elsa.Secrets.Stores;
using Elsa.Secrets.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elsa.Secrets.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSecretsServices(this IServiceCollection services, Action<SecretsOptions>? configureOptions = null)
    {
        if (configureOptions != null)
            services.Configure(configureOptions);

        services.AddOptions<SecretsOptions>();
        services.TryAddSingleton<ISecretNameValidator, DefaultSecretNameValidator>();
        services.TryAddSingleton<ISecretRepository, FileSecretRepository>();
        services.TryAddSingleton<ISecretValueProtector, DefaultSecretValueProtector>();
        services.TryAddSingleton<ISecretManager, DefaultSecretManager>();
        services.TryAddSingleton<ISecretResolver, DefaultSecretResolver>();
        services.TryAddSingleton<ISecretStoreRegistry, SecretStoreRegistry>();
        services.TryAddSingleton<ISecretTypeRegistry, SecretTypeRegistry>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IExpressionDescriptorProvider, SecretExpressionDescriptorProvider>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ISecretStore, EncryptedSecretStore>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ISecretStore, ConfigurationSecretStore>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ISecretTypeProvider, TextSecretTypeProvider>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ISecretTypeProvider, RsaKeySecretTypeProvider>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ISecretTypeProvider, X509CertificateSecretTypeProvider>());

        return services;
    }
}
