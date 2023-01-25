using System.Reflection;
using Elsa.Http.Options;
using Elsa.Workflows.Management.Implementations;
using Microsoft.Extensions.Options;

namespace Elsa.Http.Providers;

internal class WriteHttpResponseContentTypeOptionsProvider : IActivityPropertyOptionsProvider
{
    private readonly HttpActivityOptions _options;

    public WriteHttpResponseContentTypeOptionsProvider(IOptions<HttpActivityOptions> options)
    {
        _options = options.Value;
    }

    public object GetOptions(PropertyInfo property) => new[] { "" }.Concat(_options.AvailableContentTypes);
}