using System.Collections.Generic;

using Elsa.Models;

using NodaTime;

namespace Elsa.Indexing.Models
{
    public class WorkflowInstanceIndexModel
    {
        public string Id { get; set; } = default!;
        public string DefinitionId { get; set; } = default!;

        public string? TenantId { get; set; }

        public int Version { get; set; }

        public WorkflowStatus WorkflowStatus { get; set; }

        public string? CorrelationId { get; set; }

        public string? ContextType { get; set; }

        public string? ContextId { get; set; }

        public string? Name { get; set; }

        public Instant CreatedAt { get; set; }

        public Instant? LastExecutedAt { get; set; }

        public Instant? FinishedAt { get; set; }

        public Instant? CancelledAt { get; set; }

        public Instant? FaultedAt { get; set; }

        public Instant? LastSavedAt { get; set; }

        public List<ActivityDefinitionIndexModel> Activities { get; set; } = new List<ActivityDefinitionIndexModel>();
    }
}
