using Newtonsoft.Json.Linq;

namespace Elsa.Client.Models
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
        
        public string? Id { get; set; }
        public string? Type { get; set; }
        public JObject Data { get; set; } = new JObject();
        public object? Output { get; set; }
    }
}