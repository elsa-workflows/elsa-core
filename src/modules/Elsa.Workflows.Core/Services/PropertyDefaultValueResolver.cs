using System.Reflection;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Services;

/// <inheritdoc />
public class PropertyDefaultValueResolver(IServiceScopeFactory scopeFactory) : IPropertyDefaultValueResolver
{
    /// <inheritdoc />
    public object? GetDefaultValue(PropertyInfo activityPropertyInfo)
    {
        var inputAttribute = activityPropertyInfo.GetCustomAttribute<InputAttribute>();

        if (inputAttribute == null)
            return null;

        if (inputAttribute.DefaultValueProvider == null)
            return inputAttribute.DefaultValue;

        var providerType = inputAttribute.DefaultValueProvider;
        using var scope = scopeFactory.CreateScope();
        var provider = (IActivityPropertyDefaultValueProvider) ActivatorUtilities.GetServiceOrCreateInstance(scope.ServiceProvider, providerType);
        return provider.GetDefaultValue(activityPropertyInfo);
    }
}