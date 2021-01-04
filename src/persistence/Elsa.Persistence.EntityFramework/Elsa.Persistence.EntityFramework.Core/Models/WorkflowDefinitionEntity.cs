namespace Elsa.Persistence.EntityFramework.Core.Models
{
    public class WorkflowDefinitionEntity
    {
        public string Id { get; set; } = default!;
        public string DefinitionVersionId { get; set; } = default!;
        public string? TenantId { get; set; }
        public string? Name { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsPublished { get; set; }
        public bool IsLatest { get; set; }
        public string? Data { get; set; }        
    }
}