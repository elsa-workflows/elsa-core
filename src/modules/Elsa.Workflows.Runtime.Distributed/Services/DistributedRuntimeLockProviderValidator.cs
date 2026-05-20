using System.Reflection;
using Elsa.Common.DistributedHosting;
using Elsa.Common.DistributedHosting.DistributedLocks;
using Medallion.Threading;
using Medallion.Threading.FileSystem;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Runtime.Distributed;

/// <summary>
/// Validates that the distributed runtime is not accidentally backed by a local-only lock provider.
/// </summary>
public class DistributedRuntimeLockProviderValidator(
    IDistributedLockProvider distributedLockProvider,
    IOptions<DistributedLockingOptions> options,
    ILogger<DistributedRuntimeLockProviderValidator> logger)
{
    /// <summary>
    /// Validates the configured provider.
    /// </summary>
    public void Validate()
    {
        var localProviderType = FindLocalProviderType(distributedLockProvider);
        var configuredProviderTypeName = distributedLockProvider.GetType().FullName ?? distributedLockProvider.GetType().Name;

        if (localProviderType == null)
        {
            logger.LogInformation(
                "Distributed workflow runtime lock provider {DistributedLockProviderType} is configured. Ensure this provider coordinates across all application nodes.",
                configuredProviderTypeName);
            return;
        }

        var localProviderTypeName = localProviderType.FullName ?? localProviderType.Name;

        if (options.Value.AllowLocalLockProviderInDistributedRuntime)
        {
            logger.LogWarning(
                "Distributed workflow runtime is using local-only lock provider {LocalDistributedLockProviderType} through configured provider {DistributedLockProviderType}. This is safe only for single-host deployments.",
                localProviderTypeName,
                configuredProviderTypeName);
            return;
        }

        var message =
            $"The distributed workflow runtime is configured with local-only distributed lock provider '{localProviderTypeName}' through '{configuredProviderTypeName}'. " +
            "This provider does not coordinate across nodes with separate file systems and can allow concurrent workflow processing in clustered deployments. " +
            "Configure a cross-node IDistributedLockProvider such as Redis, SQL Server, or PostgreSQL, or explicitly set DistributedLockingOptions.AllowLocalLockProviderInDistributedRuntime to true for single-host development/test deployments.";

        throw new InvalidOperationException(message);
    }

    private Type? FindLocalProviderType(IDistributedLockProvider provider, ISet<IDistributedLockProvider>? visited = null)
    {
        visited ??= new HashSet<IDistributedLockProvider>(ReferenceEqualityComparer.Instance);

        if (!visited.Add(provider))
            return null;

        var providerType = provider.GetType();

        if (provider is FileDistributedSynchronizationProvider or NoopDistributedSynchronizationProvider)
            return providerType;

        foreach (var innerProvider in GetInnerProviders(provider))
        {
            var localProviderType = FindLocalProviderType(innerProvider, visited);

            if (localProviderType != null)
                return localProviderType;
        }

        return null;
    }

    private IReadOnlyCollection<IDistributedLockProvider> GetInnerProviders(IDistributedLockProvider provider)
    {
        var providers = new List<IDistributedLockProvider>();

        foreach (var property in provider.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (property.GetIndexParameters().Length > 0)
                continue;

            var returnsDistributedLockProvider = typeof(IDistributedLockProvider).IsAssignableFrom(property.PropertyType);
            var returnsDistributedLockProviders = typeof(IEnumerable<IDistributedLockProvider>).IsAssignableFrom(property.PropertyType);

            if (!returnsDistributedLockProvider && !returnsDistributedLockProviders)
                continue;

            object? value;
            try
            {
                value = property.GetValue(provider);
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Skipping distributed lock provider property {PropertyName} because the getter threw.", property.Name);
                continue;
            }

            if (value is IDistributedLockProvider innerProvider)
                providers.Add(innerProvider);
            else if (value is IEnumerable<IDistributedLockProvider> innerProviders)
            {
                try
                {
                    var providerItems = new List<IDistributedLockProvider>();

                    foreach (var providerItem in innerProviders)
                    {
                        if (providerItem != null)
                            providerItems.Add(providerItem);
                    }

                    providers.AddRange(providerItems);
                }
                catch (Exception ex)
                {
                    logger.LogDebug(ex, "Skipping distributed lock provider property {PropertyName} because enumeration threw.", property.Name);
                }
            }
        }

        return providers;
    }
}
