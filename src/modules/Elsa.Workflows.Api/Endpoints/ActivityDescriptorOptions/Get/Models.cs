using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.ActivityDescriptorOptions.Get;

internal class Request
{
    
    public string ActivityTypeName { get; set; } = default!;
    public int? Version { get; set; }
    public string PropertyName { get; set; } = default!;
    
    public object? Context { get; set; }
}

[PublicAPI]
internal class Response
{
    public Response(IDictionary<string,object> items)
    {
        Items = items;
    }

    public IDictionary<string, object> Items { get; set; }
}