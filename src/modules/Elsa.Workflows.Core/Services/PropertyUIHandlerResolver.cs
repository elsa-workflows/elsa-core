using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Contracts;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Elsa.Workflows.Core.Services;

/// <inheritdoc />
public class PropertyUIHandlerResolver(IServiceProvider serviceProvider) : IPropertyUIHandlerResolver
{
    /// <inheritdoc />
    public async ValueTask<IDictionary<string, object?>> GetUIProperties(PropertyInfo propertyInfo, object? context, CancellationToken cancellationToken = default)
    {
        var inputAttribute = propertyInfo.GetCustomAttribute<InputAttribute>();
        var result = new Dictionary<string, object?>();

        if (inputAttribute?.UIHandler == null)
            return result;

        foreach (var handlerType in inputAttribute.UIHandler)
        {
            using var scope = serviceProvider.CreateScope();
            var provider = (IPropertyUIHandler)ActivatorUtilities.GetServiceOrCreateInstance(scope.ServiceProvider, handlerType);
            var properties = await provider.GetUIPropertiesAsync(propertyInfo, context, cancellationToken);

            result.Add(provider.Name, properties);
        }

        return result;
    }
}