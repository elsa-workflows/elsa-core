using Elsa.Common.Multitenancy;

namespace Elsa.Tenants;

/// <summary>
/// Represents a pipeline builder of tenant resolution strategies.
/// </summary>
public interface ITenantResolverPipelineBuilder
{
    /// <summary>
    /// Contains strategies for resolving tenants.
    /// </summary>
    IEnumerable<Type> Resolvers { get; }
    
    /// <summary>
    /// Appends a new strategy to the pipeline. The last appended strategy will be the first to be executed.
    /// </summary>
    ITenantResolverPipelineBuilder Append<T>() where T : ITenantResolver;
    
    /// <summary>
    /// Appends a new strategy to the pipeline. The last appended strategy will be the first to be executed.
    /// </summary>
    ITenantResolverPipelineBuilder Append(Type resolverType);
    
    /// <summary>
    /// Clears the pipeline.
    /// </summary>
    ITenantResolverPipelineBuilder Clear();
    
    /// <summary>
    /// Builds the pipeline.
    /// </summary>
    IEnumerable<ITenantResolver> Build(IServiceProvider serviceProvider);
    
}