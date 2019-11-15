using Newtonsoft.Json.Linq;

namespace Elsa.Persistence.EntityFrameworkCore.Entities
{
    public class ActivityDefinitionEntity
    {
        public string Id { get; set; }
        public WorkflowDefinitionVersionEntity WorkflowDefinitionVersion { get; set; }
        public string Type { get; set; }
        public int Left { get; set; }
        public int Top { get; set; }
        public JObject State { get; set; }
    }
}