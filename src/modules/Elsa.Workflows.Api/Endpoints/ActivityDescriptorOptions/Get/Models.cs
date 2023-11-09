using JetBrains.Annotations;

namespace Elsa.Workflows.Api.Endpoints.ActivityDescriptorOptions.Get;

internal class Request
{
    public string TypeName { get; set; }
    public int? Version { get; set; }
    public string PropertyName { get; set; }
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