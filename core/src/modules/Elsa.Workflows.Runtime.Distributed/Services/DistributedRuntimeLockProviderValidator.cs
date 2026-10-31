using System.Linq;
using System.Reflection;
using Elsa.Common.DistributedHosting;
using Elsa.Common.DistributedHosting.DistributedLocks;
using Medallion.Threading;
using Medallion.Threading.FileSystem;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Runtime.Distributed;

/// <summary>
/// Logs when the distributed runtime is backed by a local-only lock provider.
/// </summary>
public class DistributedRuntimeLockProviderValidator(
    IDistributedLockProvider distributedLockProvider,
    IOptions<DistributedLockingOptions> options,
    ILogger<DistributedRuntimeLockProviderValidator> logger)
{
    /// <summary>
    /// Checks the configured provider.
    /// </summary>
    public void Validate()
    {
        var localProviderType = FindLocalProviderType(distributedLockProvider);
        var configuredProviderTypeName = distributedLockProvider.GetType().FullName ?? distributedLockProvider.GetType().Name;

        if (localProviderType == null)
        {
            logger.LogDebug(
                "Distributed workflow runtime lock provider {DistributedLockProviderType} is configured. Ensure this provider coordinates across all application nodes.",
                configuredProviderTypeName);
            return;
        }

        var localProviderTypeName = localProviderType.FullName ?? localProviderType.Name;

        if (options.Value.AllowLocalLockProviderInDistributedRuntime)
        {
            logger.LogDebug(
                "Distributed workflow runtime is using acknowledged local-only lock provider {LocalDistributedLockProviderType} through configured provider {DistributedLockProviderType}.",
                localProviderTypeName,
                configuredProviderTypeName);
            return;
        }

        logger.LogWarning(
            "Distributed workflow runtime is using local-only lock provider {LocalDistributedLockProviderType} through configured provider {DistributedLockProviderType}. This provider does not coordinate across application nodes and can allow concurrent workflow processing in clustered deployments. Configure a cross-node IDistributedLockProvider such as Redis, SQL Server, or PostgreSQL for clustered deployments, or set DistributedLockingOptions.AllowLocalLockProviderInDistributedRuntime to true to acknowledge single-host development/test usage.",
            localProviderTypeName,
            configuredProviderTypeName);
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

    private IEnumerable<IDistributedLockProvider> GetInnerProviders(IDistributedLockProvider provider)
    {
        foreach (var property in provider.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => property.GetIndexParameters().Length == 0))
        {
            var returnsDistributedLockProvider = typeof(IDistributedLockProvider).IsAssignableFrom(property.PropertyType);
            var returnsDistributedLockProviders =
                property.PropertyType == typeof(System.Collections.IEnumerable) ||
                TryGetDistributedLockProviderElementType(property.PropertyType, out _);

            if (!returnsDistributedLockProvider && !returnsDistributedLockProviders)
                continue;

            object? value;
            try
            {
                value = property.GetValue(provider);
            }
            catch (TargetInvocationException ex)
            {
                logger.LogDebug(ex, "Skipping distributed lock provider property {PropertyName} because the getter threw.", property.Name);
                continue;
            }

            if (value is IDistributedLockProvider innerProvider)
            {
                yield return innerProvider;
            }
            else if (value is System.Collections.IEnumerable innerProviders)
            {
                System.Collections.IEnumerator enumerator;

                try
                {
                    enumerator = innerProviders.GetEnumerator();
                }
                catch (Exception ex) when (ex is InvalidOperationException or ObjectDisposedException or NotSupportedException or TargetInvocationException)
                {
                    logger.LogDebug(ex, "Skipping distributed lock provider property {PropertyName} because enumeration threw.", property.Name);
                    continue;
                }

                using var disposableEnumerator = enumerator as IDisposable;

                while (true)
                {
                    object? providerItem;

                    try
                    {
                        if (!enumerator.MoveNext())
                            break;

                        providerItem = enumerator.Current;
                    }
                    catch (Exception ex) when (ex is InvalidOperationException or ObjectDisposedException or NotSupportedException)
                    {
                        logger.LogDebug(ex, "Skipping distributed lock provider property {PropertyName} because enumeration threw.", property.Name);
                        break;
                    }

                    if (providerItem is IDistributedLockProvider distributedLockProvider)
                        yield return distributedLockProvider;
                }
            }
        }
    }

    private static bool TryGetDistributedLockProviderElementType(Type type, out Type? elementType)
    {
        elementType = null;

        if (type.IsArray)
        {
            elementType = type.GetElementType();
            return elementType != null && typeof(IDistributedLockProvider).IsAssignableFrom(elementType);
        }

        if (type == typeof(string))
            return false;

        elementType = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>)
            ? type.GetGenericArguments()[0]
            : type
                .GetInterfaces()
                .Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                .Select(x => x.GetGenericArguments()[0])
                .FirstOrDefault(x => typeof(IDistributedLockProvider).IsAssignableFrom(x));

        return elementType != null && typeof(IDistributedLockProvider).IsAssignableFrom(elementType);
    }
}
