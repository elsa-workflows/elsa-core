namespace Elsa.Persistence.EntityFrameworkCore.Entities
{
    public class ConnectionDefinitionEntity
    {
        public int Id { get; set; }
        public WorkflowDefinitionVersionEntity WorkflowDefinitionVersion { get; set; }
        public string SourceActivityId { get; set; }
        public string? TargetActivityId { get; set; }
        public string Outcome { get; set; }
    }
}