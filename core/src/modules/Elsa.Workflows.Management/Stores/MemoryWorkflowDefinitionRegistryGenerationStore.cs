using System.Collections.Concurrent;
using Elsa.Common.Multitenancy;
using Elsa.Workflows.Management.Contracts;

namespace Elsa.Workflows.Management.Stores;

/// <summary>
/// An in-memory registry generation store for single-node use and tests.
/// </summary>
public class MemoryWorkflowDefinitionRegistryGenerationStore : IWorkflowDefinitionRegistryGenerationStore
{
    private readonly ConcurrentDictionary<string, long> _generations = new();

    /// <inheritdoc />
    public bool IsShared => false;

    /// <inheritdoc />
    public Task<long> IncrementAsync(string? tenantId, CancellationToken cancellationToken = default)
    {
        var generation = _generations.AddOrUpdate(NormalizeTenantId(tenantId), 1, (_, current) => current + 1);
        return Task.FromResult(generation);
    }

    /// <inheritdoc />
    public Task<long> GetGenerationAsync(string? tenantId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_generations.GetValueOrDefault(NormalizeTenantId(tenantId)));
    }

    /// <inheritdoc />
    private static string NormalizeTenantId(string? tenantId) => tenantId ?? Tenant.DefaultTenantId;
}
