namespace Elsa.Common.Multitenancy;

/// <summary>Provides tenant hints explicitly supplied by the incoming request.</summary>
public interface ITenantRequestHints
{
    /// <summary>Returns distinct normalized tenant IDs from enabled request hint resolvers.</summary>
    Task<IReadOnlyCollection<string>> GetAsync(CancellationToken cancellationToken = default);
}

/// <summary>Identifies tenant resolvers that read request hints independently of the authenticated principal.</summary>
public interface ITenantRequestHintResolver : ITenantResolver
{
    /// <summary>Returns every explicit tenant ID represented by this request hint.</summary>
    async Task<IReadOnlyCollection<string>> GetHintsAsync(TenantResolverContext context)
    {
        var result = await ResolveAsync(context);
        return result.IsResolved ? [result.ResolveTenantId()] : [];
    }
}
