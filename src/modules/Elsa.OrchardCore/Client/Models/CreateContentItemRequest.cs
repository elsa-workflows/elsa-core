using System.Text.Json.Nodes;

namespace Elsa.OrchardCore.Client;

public class CreateContentItemRequest
{
    public string ContentType { get; set; } = default!;
    public JsonNode Properties { get; set; } = default!;
    public bool Publish { get; set; }
}