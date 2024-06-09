using Elsa.Framework.Tenants;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Tenants.Services;

/// <inheritdoc />
public class TenantResolutionPipelineBuilder : ITenantResolutionPipelineBuilder
{
    private readonly ISet<Type> _resolvers = new HashSet<Type>();

    /// <inheritdoc />
    public IEnumerable<Type> Resolvers => _resolvers.ToList().AsReadOnly();

    /// <inheritdoc />
    public ITenantResolutionPipelineBuilder Append<T>() where T : ITenantResolutionStrategy
    {
        var strategyType = typeof(T);
        return Append(strategyType);
    }

    /// <inheritdoc />
    public ITenantResolutionPipelineBuilder Append(Type resolverType)
    {
        if (typeof(ITenantResolutionStrategy).IsAssignableFrom(resolverType) == false)
            throw new ArgumentException($"The type {resolverType} does not implement {nameof(ITenantResolutionStrategy)}.");

        _resolvers.Add(resolverType);
        return this;
    }

    /// <inheritdoc />
    public ITenantResolutionPipelineBuilder Clear()
    {
        _resolvers.Clear();
        return this;
    }

    /// <inheritdoc />
    public IEnumerable<ITenantResolutionStrategy> Build(IServiceProvider serviceProvider)
    {
        return _resolvers
            .Reverse()
            .Select(t => ActivatorUtilities.GetServiceOrCreateInstance(serviceProvider, t))
            .Cast<ITenantResolutionStrategy>()
            .ToList();
    }
}