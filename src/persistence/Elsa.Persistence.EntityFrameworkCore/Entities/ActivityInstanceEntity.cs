using Newtonsoft.Json.Linq;

namespace Elsa.Persistence.EntityFrameworkCore.Entities
{
    public class ActivityInstanceEntity
    {
        public string Id { get; set; }
        public WorkflowInstanceEntity WorkflowInstance { get; set; }
        public string Type { get; set; }
        public JObject State { get; set; }
        public JObject Output { get; set; }
    }
}