using System;

namespace Elsa.Persistence.YesSql.Documents
{
    public class WorkflowDefinitionDocument : YesSqlDocument
    {
        public string Id { get; set; }
        public string TenantId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
