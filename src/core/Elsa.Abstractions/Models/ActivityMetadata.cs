using Newtonsoft.Json.Linq;

namespace Elsa.Models
{
    public class ActivityMetadata
    {
        public string Title { get; set; }
        public JObject CustomFields { get; set; } = new JObject();
    }
}