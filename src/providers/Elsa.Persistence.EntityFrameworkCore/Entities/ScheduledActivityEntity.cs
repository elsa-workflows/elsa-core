using Elsa.Models;

namespace Elsa.Persistence.EntityFrameworkCore.Entities
{
    public class ScheduledActivityEntity
    {
        public int Id { get; set; }
        public WorkflowInstanceEntity WorkflowInstance { get; set; }
        public string ActivityId { get; set; }
        public Variable? Input { get; set; }
    }
}