using Newtonsoft.Json.Linq;

namespace Elsa.Client.Models
{
    public class ActivityPropertyDescriptor
    {
        public string Name { get; } = default!;
        public string Type { get; } = default!;
        public string? Label { get; set; }
        public string? Hint { get; set;}
        public JObject? Options { get; set;}
    }
}