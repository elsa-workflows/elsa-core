using Elsa.Common.Contexts;
using Elsa.Common.Results;

namespace Elsa.Common.Contracts;

/// <summary>
/// A tenant resolver strategy in a pipeline of tenant resolvers.
/// </summary>
public interface ITenantResolutionStrategy
{
    /// <summary>
    /// Resolves the tenant.
    /// </summary>
    ValueTask<TenantResolutionResult> ResolveAsync(TenantResolutionContext context);
}