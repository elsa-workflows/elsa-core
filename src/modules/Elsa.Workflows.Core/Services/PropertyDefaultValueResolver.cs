using System.Reflection;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Core.Services;

public class PropertyDefaultValueResolver : IPropertyDefaultValueResolver
{
    private readonly IServiceProvider _serviceProvider;

    public PropertyDefaultValueResolver(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public object? GetDefaultValue(PropertyInfo activityPropertyInfo)
    {
        var inputAttribute = activityPropertyInfo.GetCustomAttribute<InputAttribute>();

        if (inputAttribute == null)
            return null;

        if (inputAttribute.DefaultValueProvider == null)
            return inputAttribute.DefaultValue;

        var providerType = inputAttribute.DefaultValueProvider;

        using var scope = _serviceProvider.CreateScope();
        var provider = (IActivityPropertyDefaultValueProvider) ActivatorUtilities.GetServiceOrCreateInstance(scope.ServiceProvider, providerType);
        return provider.GetDefaultValue(activityPropertyInfo);
    }
}