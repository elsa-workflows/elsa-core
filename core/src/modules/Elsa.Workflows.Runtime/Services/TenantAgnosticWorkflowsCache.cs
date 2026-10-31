using System.Collections.Concurrent;

namespace Elsa.Workflows.Runtime;

/// <summary>
/// Caches the built and serialized workflows of <see cref="ITenantAgnosticWorkflowsProvider"/>s, so that a service
/// container builds them only once.
/// </summary>
internal class TenantAgnosticWorkflowsCache
{
    private readonly ConcurrentDictionary<Type, IReadOnlyList<ImportedWorkflow>> _cache = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    /// <summary>
    /// Returns the workflows of the specified provider type. A build is remembered only when it is allowed to be cached
    /// and all of its workflows are tenant-agnostic. A failed build is not remembered.
    /// </summary>
    /// <param name="providerType">The type of the workflows provider.</param>
    /// <param name="build">Builds the workflows of the provider.</param>
    /// <param name="cacheBuild">Whether a new build may be remembered.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    public async Task<IReadOnlyList<ImportedWorkflow>> GetOrBuildAsync(Type providerType, Func<Task<IReadOnlyList<ImportedWorkflow>>> build, bool cacheBuild, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(providerType, out var cached))
        {
            return cached;
        }

        if (!cacheBuild)
        {
            return await build();
        }

        await _semaphore.WaitAsync(cancellationToken);

        try
        {
            if (_cache.TryGetValue(providerType, out cached))
            {
                return cached;
            }

            var workflows = await build();

            if (workflows.All(x => x.IsTenantAgnostic))
            {
                _cache[providerType] = workflows;
            }

            return workflows;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
