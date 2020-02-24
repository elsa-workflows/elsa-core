using Newtonsoft.Json.Linq;

namespace Elsa.Metadata
{
    public class ActivityPropertyDescriptor
    {
        public ActivityPropertyDescriptor(string name, string type, string label, string? hint = null, JObject? options = null)
        {
            Name = name;
            Type = type;
            Label = label;
            Hint = hint;
            Options = options;
        }
        
        public string Name { get; }
        public string Type { get; }
        public string? Label { get; }
        public string? Hint { get; }
        public JObject? Options { get; }
    }
}