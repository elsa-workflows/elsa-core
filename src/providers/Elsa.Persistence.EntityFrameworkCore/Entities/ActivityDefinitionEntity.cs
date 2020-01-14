using Newtonsoft.Json.Linq;

namespace Elsa.Persistence.EntityFrameworkCore.Entities
{
    public class ActivityDefinitionEntity
    {
        public int Id { get; set; }
        public string ActivityId { get; set; }
        public WorkflowDefinitionVersionEntity WorkflowDefinitionVersion { get; set; }
        public string Type { get; set; }
        public int Left { get; set; }
        public int Top { get; set; }
        public JObject State { get; set; }
    }
}