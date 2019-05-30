using Newtonsoft.Json.Linq;

namespace Elsa.Models
{
    public class WorkflowMetadata
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public JObject CustomFields { get; set; }
    }
}