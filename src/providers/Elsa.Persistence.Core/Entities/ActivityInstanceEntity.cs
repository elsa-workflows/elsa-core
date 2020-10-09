using Elsa.Models;

namespace Elsa.Persistence.Core.Entities
{
    public class ActivityInstanceEntity
    {
        public int Id { get; set; }
        public string ActivityId { get; set; }= default!;
        public WorkflowInstanceEntity WorkflowInstance { get; set; }= default!;
        public string Type { get; set; }= default!;
        public Variables State { get; set; }= default!;
        public Variables Output { get; set; }= default!;
    }
}