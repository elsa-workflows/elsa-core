using Newtonsoft.Json.Linq;

namespace Elsa.Metadata
{
    public record ActivityPropertyDescriptor(string Name, string UIHint, string Label, string? Hint = null, object? Options = null)
    {
    }
}