using Newtonsoft.Json.Linq;

namespace Elsa.Metadata
{
    public record ActivityPropertyDescriptor(string Name, string Type, string Label, string? Hint = null, JObject? Options = null)
    {
    }
}