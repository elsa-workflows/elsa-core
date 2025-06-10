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

        // Handlers are sorted by priority so that those with higher priority are processed last. 
        // This allows them to override any existing property values set by lower-priority handlers.
        // For example, a default UI hint handler might provide dropdown options, but a custom module can supply its own handler with a higher priority to override these defaults.
        var availablePropertyUIHandlers = scope.ServiceProvider.GetServices<IPropertyUIHandler>().OrderBy(x => x.Priority).ToList();
        var matchedPropertyHandlers = availablePropertyUIHandlers.Where(x => uiHandlers.Contains(x.GetType())).OrderBy(x => x.Priority).ToList();
        
        foreach (var handler in matchedPropertyHandlers)
        {
            var properties = await handler.GetUIPropertiesAsync(propertyInfo, context, cancellationToken);

            foreach (var property in properties)
                result[property.Key] = property.Value;
        }

        return result;
    }
}