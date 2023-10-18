using System.Reflection;
using Elsa.Http.ContentWriters;
using Elsa.Workflows.Core.Contracts;

namespace Elsa.Http.ActivityOptionProviders;

/// <summary>
/// Provides options for the <see cref="SendHttpRequest"/> activity's <see cref="SendHttpRequest.ContentType"/> property.
/// </summary>
public class HttpContentTypeOptionsProvider : IActivityPropertyOptionsProvider
{
    private readonly IEnumerable<IHttpContentFactory> _httpContentFactories;

    /// <summary>
    /// Creates a new instance of the <see cref="HttpContentTypeOptionsProvider"/> class.
    /// </summary>
    public HttpContentTypeOptionsProvider(IEnumerable<IHttpContentFactory> httpContentFactories)
    {
        _httpContentFactories = httpContentFactories;
    }

    /// <inheritdoc />
    public ValueTask<IDictionary<string, object>> GetOptionsAsync(PropertyInfo property, CancellationToken cancellationToken = default)
    {
        var contentTypes = _httpContentFactories.SelectMany(x => x.SupportedContentTypes).Distinct().OrderBy(x => x).ToArray();

        var options = new Dictionary<string, object>
        {
            ["items"] = new[] { "" }.Concat(contentTypes)
        };

        return new(options);
    }
}