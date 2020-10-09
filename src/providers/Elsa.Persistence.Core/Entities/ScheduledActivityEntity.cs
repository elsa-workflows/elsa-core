namespace Elsa.Persistence.Core.Entities
{
    public class ScheduledActivityEntity
    {
        public int Id { get; set; }
        public WorkflowInstanceEntity WorkflowInstance { get; set; } = default!;
        public string ActivityId { get; set; } = default!;
        public object? Input { get; set; }
    }
}