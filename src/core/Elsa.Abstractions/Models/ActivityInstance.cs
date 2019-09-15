using Newtonsoft.Json.Linq;

namespace Elsa.Models
{
    public class ActivityInstance
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public JObject State { get; set; }
        public JObject Output { get; set; }
    }
}