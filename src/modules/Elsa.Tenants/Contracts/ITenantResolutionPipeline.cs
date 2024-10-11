using Elsa.Common.Multitenancy;

namespace Elsa.Tenants;

/// <summary>
/// Represents a pipeline builder of tenant resolution strategies.
/// </summary>
public interface ITenantResolutionPipelineBuilder
{
    /// <summary>
    /// Contains strategies for resolving tenants.
    /// </summary>
    IEnumerable<Type> Resolvers { get; }
    
    /// <summary>
    /// Appends a new strategy to the pipeline. The last appended strategy will be the first to be executed.
    /// </summary>
    ITenantResolutionPipelineBuilder Append<T>() where T : ITenantResolutionStrategy;
    
    /// <summary>
    /// Appends a new strategy to the pipeline. The last appended strategy will be the first to be executed.
    /// </summary>
    ITenantResolutionPipelineBuilder Append(Type resolverType);
    
    /// <summary>
    /// Clears the pipeline.
    /// </summary>
    ITenantResolutionPipelineBuilder Clear();
    
    /// <summary>
    /// Builds the pipeline.
    /// </summary>
    IEnumerable<ITenantResolutionStrategy> Build(IServiceProvider serviceProvider);
    
}