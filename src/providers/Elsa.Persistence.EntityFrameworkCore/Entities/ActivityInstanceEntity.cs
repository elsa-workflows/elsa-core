using Elsa.Models;

namespace Elsa.Persistence.EntityFrameworkCore.Entities
{
    public class ActivityInstanceEntity
    {
        public int Id { get; set; }
        public string ActivityId { get; set; }
        public WorkflowInstanceEntity WorkflowInstance { get; set; }
        public string Type { get; set; }
        public Variables State { get; set; }
        // This is commented out until we figure out mappings in elsa-2.0
        //public Variables Output { get; set; }
        public Variable? Output { get; set; }
    }
}