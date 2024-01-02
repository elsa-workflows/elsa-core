using System.Reflection; 
using Elsa.Workflows.Attributes; 
using Elsa.Workflows.Contracts; 
using Microsoft.Extensions.DependencyInjection; 
 
namespace Elsa.Workflows.Services; 
 
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
         
        if (!string.IsNullOrWhiteSpace(inputAttribute?.UIHint)) 
        { 
            var uiHintHandler = uiHintHandlers.FirstOrDefault(x => x.UIHint == inputAttribute.UIHint); 
 
            if (uiHintHandler != null) 
            { 
                var defaultHandlers = await uiHintHandler.GetPropertyUIHandlersAsync(propertyInfo, cancellationToken); 
                uiHandlers.AddRange(defaultHandlers); 
            } 
        } 
 
        foreach (var handlerType in uiHandlers) 
        { 
            var provider = (IPropertyUIHandler)ActivatorUtilities.GetServiceOrCreateInstance(scope.ServiceProvider, handlerType); 
            var properties = await provider.GetUIPropertiesAsync(propertyInfo, context, cancellationToken); 
 
            foreach (var property in properties) 
                result[property.Key] = property.Value; 
        } 
 
        return result; 
    } 
}