using System.Reflection;
using Elsa.Workflows;
using Elsa.Workflows.UIHints;
using Elsa.Workflows.UIHints.SingleLine;

namespace Elsa.Http.UIHints;

/// <summary>
/// Provides additional options for the Path input field.
/// </summary>
public class HttpEndpointPathUIHandler(IHttpEndpointBasePathProvider httpEndpointBasePathProvider) : IPropertyUIHandler
{
    /// <inheritdoc />
    public ValueTask<IDictionary<string, object>> GetUIPropertiesAsync(PropertyInfo propertyInfo, object? context, CancellationToken cancellationToken = default)
    {
        var completeBaseUrl = httpEndpointBasePathProvider.GetBasePath();

        return new(new Dictionary<string, object>
        {
            [InputUIHints.SingleLine] = new SingleLineProps
            {
                AdornmentText = completeBaseUrl,
                EnableCopyAdornment = true
            },
            ["Refresh"] = true
        });
    }
}