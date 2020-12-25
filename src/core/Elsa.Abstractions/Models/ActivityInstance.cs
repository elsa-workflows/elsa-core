using Newtonsoft.Json.Linq;

namespace Elsa.Models
{
    public class ActivityInstance
    {
        public ActivityInstance()
        {
        }

        public ActivityInstance(string id, string type, object? output, JObject data)
        {
            Id = id;
            Type = type;
            Data = data;
            Output = output;
        }

        public string Id { get; set; } = default!;
        public string Type { get; set; } = default!;
        public JObject Data { get; set; } = new();
        public object? Output { get; set; }

        public override string ToString() => $"{Type} - {Id}";
    }
}