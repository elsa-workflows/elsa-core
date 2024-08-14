using System.Text.Json.Nodes;

namespace Elsa.OrchardCore.Client;

public class PatchContentItemRequest
{
    public JsonNode Patch { get; set; } = default!;
    public bool Publish { get; set; }
}