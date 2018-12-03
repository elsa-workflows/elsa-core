using Newtonsoft.Json.Linq;

namespace Flowsharp.Models
{
    public class ActivityMetadata
    {
        public JObject CustomFields { get; set; } = new JObject();
    }
}