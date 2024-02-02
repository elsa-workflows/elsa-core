using System.Reflection;
using Elsa.Workflows.Contracts;

namespace Elsa.Samples.AspNet.CustomUIHandler;

/// <summary>
/// Configures the specified property to refresh the UI when the property value changes.
/// </summary>
public class RefreshUIHandler : IPropertyUIHandler
{
    public ValueTask<IDictionary<string, object>> GetUIPropertiesAsync(PropertyInfo propertyInfo, object? context, CancellationToken cancellationToken = default)
    {
        IDictionary<string, object> result = new Dictionary<string, object>
        {
            { "Refresh", true }
        };
        return ValueTask.FromResult(result);
    }
}