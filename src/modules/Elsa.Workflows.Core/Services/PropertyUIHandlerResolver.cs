using System.Reflection;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows;

/// <inheritdoc /> 
public class PropertyUIHandlerResolver(IServiceScopeFactory scopeFactory) : IPropertyUIHandlerResolver
{
    /// <inheritdoc /> 
    public async ValueTask<IDictionary<string, object>> GetUIPropertiesAsync(PropertyInfo propertyInfo, object? context, CancellationToken cancellationToken = default)
    {
        var inputAttribute = propertyInfo.GetCustomAttribute<InputAttribute>();
        var result = new Dictionary<string, object>();

        var uiHandlers = new List<Type>();

        if (inputAttribute?.UIHandler != null)
            uiHandlers.Add(inputAttribute.UIHandler);

        if (inputAttribute?.UIHandlers != null)
            uiHandlers.AddRange(inputAttribute.UIHandlers);

        using var scope = scopeFactory.CreateScope();
        var uiHintHandlers = scope.ServiceProvider.GetServices<IUIHintHandler>();

        var isWrapperProperty = typeof(Input).IsAssignableFrom(propertyInfo.PropertyType);
        var wrapperPropertyType = !isWrapperProperty ? propertyInfo.PropertyType : propertyInfo.PropertyType.GenericTypeArguments[0];
        var uiHint = ActivityDescriber.GetUIHint(wrapperPropertyType, inputAttribute);
        if (!string.IsNullOrWhiteSpace(uiHint))
        {
            var uiHintHandler = uiHintHandlers.FirstOrDefault(x => x.UIHint == uiHint);

            if (uiHintHandler != null)
            {
                var defaultHandlers = await uiHintHandler.GetPropertyUIHandlersAsync(propertyInfo, cancellationToken);
                uiHandlers.AddRange(defaultHandlers);
            }
        }

        var propertyUIHandlers = scope.ServiceProvider.GetServices<IPropertyUIHandler>().ToList();
        foreach (var handlerType in uiHandlers)
        {
            var provider = propertyUIHandlers.FirstOrDefault(x => x.GetType() == handlerType);

            if (provider == null)
                continue;

            var properties = await provider.GetUIPropertiesAsync(propertyInfo, context, cancellationToken);

            foreach (var property in properties)
                result[property.Key] = property.Value;
        }

        return result;
    }
}