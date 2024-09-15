using System.Reflection;
using Elsa.Workflows.Contracts;

// ReSharper disable once CheckNamespace
namespace Elsa.JavaScript.Activities;

/// <summary>
/// Configures the specified property to refresh the UI when the property value changes.
/// </summary>
public class DisableSyntaxSelection : IPropertyUIHandler
{
    public ValueTask<IDictionary<string, object>> GetUIPropertiesAsync(PropertyInfo propertyInfo, object? context, CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, object>
        {
            { "DisableSyntaxSelection", true }
        };
        return ValueTask.FromResult<IDictionary<string, object>>(result);
    }
}