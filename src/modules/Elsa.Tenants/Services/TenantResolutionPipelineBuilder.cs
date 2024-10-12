using Elsa.Common.Multitenancy;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Tenants.Services;

/// <inheritdoc />
public class TenantResolverPipelineBuilder : ITenantResolverPipelineBuilder
{
    private readonly ISet<Type> _resolvers = new HashSet<Type>();

    /// <inheritdoc />
    public IEnumerable<Type> Resolvers => _resolvers.ToList().AsReadOnly();

    /// <inheritdoc />
    public ITenantResolverPipelineBuilder Append<T>() where T : ITenantResolver
    {
        var strategyType = typeof(T);
        return Append(strategyType);
    }

    /// <inheritdoc />
    public ITenantResolverPipelineBuilder Append(Type resolverType)
    {
        if (typeof(ITenantResolver).IsAssignableFrom(resolverType) == false)
            throw new ArgumentException($"The type {resolverType} does not implement {nameof(ITenantResolver)}.");

        _resolvers.Add(resolverType);
        return this;
    }

    /// <inheritdoc />
    public ITenantResolverPipelineBuilder Clear()
    {
        _resolvers.Clear();
        return this;
    }

    /// <inheritdoc />
    public IEnumerable<ITenantResolver> Build(IServiceProvider serviceProvider)
    {
        return _resolvers
            .Reverse()
            .Select(t => ActivatorUtilities.GetServiceOrCreateInstance(serviceProvider, t))
            .Cast<ITenantResolver>()
            .ToList();
    }
}