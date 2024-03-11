using Elsa.Common.Contracts;
using Elsa.Tenants.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Tenants.Services;

/// <inheritdoc />
public class TenantResolutionPipelineBuilder : ITenantResolutionPipelineBuilder
{
    private readonly ISet<Type> _strategies = new HashSet<Type>();

    /// <inheritdoc />
    public IEnumerable<Type> Strategies => _strategies.ToList().AsReadOnly();

    /// <inheritdoc />
    public ITenantResolutionPipelineBuilder Append<T>() where T : ITenantResolutionStrategy
    {
        var strategyType = typeof(T);
        return Append(strategyType);
    }

    /// <inheritdoc />
    public ITenantResolutionPipelineBuilder Append(Type strategyType)
    {
        if (typeof(ITenantResolutionStrategy).IsAssignableFrom(strategyType) == false)
            throw new ArgumentException($"The type {strategyType} does not implement {nameof(ITenantResolutionStrategy)}.");

        _strategies.Add(strategyType);
        return this;
    }

    /// <inheritdoc />
    public ITenantResolutionPipelineBuilder Clear()
    {
        _strategies.Clear();
        return this;
    }

    /// <inheritdoc />
    public IEnumerable<ITenantResolutionStrategy> Build(IServiceProvider serviceProvider)
    {
        return _strategies
            .Reverse()
            .Select(t => ActivatorUtilities.GetServiceOrCreateInstance(serviceProvider, t))
            .Cast<ITenantResolutionStrategy>()
            .ToList();
    }
}