using System.Reflection;
using Elsa.Http.Options;
using Elsa.Workflows.Core.Contracts;
using Microsoft.Extensions.Options;

namespace Elsa.Http.Providers;

internal class WriteHttpResponseContentTypeOptionsProvider : IActivityPropertyOptionsProvider
{
    private readonly HttpActivityOptions _options;

    public WriteHttpResponseContentTypeOptionsProvider(IOptions<HttpActivityOptions> options)
    {
        _options = options.Value;
    }
    
    public ValueTask<IDictionary<string, object>> GetOptionsAsync(PropertyInfo property, CancellationToken cancellationToken = default)
    {
        var options = new Dictionary<string, object>
        {
            ["items"] = new[] { "" }.Concat(_options.AvailableContentTypes)
        };
        
        return new(options);
    }
}