namespace Elsa.Persistence.Core.Entities
{
    public class BlockingActivityEntity
    {
        public int Id { get; set; }
        public WorkflowInstanceEntity WorkflowInstance { get; set; }= default!;
        public string ActivityId { get; set; }= default!;
        public string ActivityType { get; set; }= default!;
        public string? Tag { get; set; }
    }
}