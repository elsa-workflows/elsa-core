using Newtonsoft.Json.Linq;

namespace Elsa.Models
{
    public class ActivityInstance
    {
        public string Id { get; set; }
        public string TypeName { get; set; }
        public JObject State { get; set; }
    }
}