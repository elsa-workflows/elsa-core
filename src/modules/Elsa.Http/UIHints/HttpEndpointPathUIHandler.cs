using System.Reflection;
using Elsa.Http.Options;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.UIHints;
using Elsa.Workflows.UIHints.SingleLine;
using Microsoft.Extensions.Options;

namespace Elsa.Http.UIHints;

/// <summary>
/// Provides additional options for the Path input field.
/// </summary>
public class HttpEndpointPathUIHandler(IOptions<HttpActivityOptions> options) : IPropertyUIHandler
{
    /// <inheritdoc />
    public ValueTask<IDictionary<string, object>> GetUIPropertiesAsync(PropertyInfo propertyInfo, object? context, CancellationToken cancellationToken = default)
    {
        var baseUrl = options.Value.BaseUrl;
        var basePath = options.Value.BasePath;
        var completeBaseUrl = new Uri(baseUrl, basePath);

        return new(new Dictionary<string, object>
        {
            [InputUIHints.SingleLine] = new SingleLineProps
            {
                AdornmentText = completeBaseUrl.ToString().TrimEnd('/') + '/'
            },
        });
    }
}