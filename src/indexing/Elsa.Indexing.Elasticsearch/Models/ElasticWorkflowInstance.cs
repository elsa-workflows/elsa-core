using System;
using Elsa.Models;

using Nest;

namespace Elsa.Indexing.Models
{
    public class ElasticWorkflowInstance : IElasticEntity
    {
        [Keyword]
        public string Id { get; set; } = default!;

        [Keyword(Normalizer = ElsaElasticsearchConsts.Normalizer)]
        public string DefinitionId { get; set; } = default!;

        [Keyword]
        public string? TenantId { get; set; }

        [Number]
        public int Version { get; set; }
     
        public WorkflowStatus WorkflowStatus { get; set; }

        [Keyword]
        public string? CorrelationId { get; set; }

        [Keyword]
        public string? ContextType { get; set; }

        [Keyword]
        public string? ContextId { get; set; }

        [Text(Analyzer = ElsaElasticsearchConsts.Analyzer, SearchAnalyzer = ElsaElasticsearchConsts.SearchAnalyzer)]
        public string? Name { get; set; }

        [Date]
        public DateTime CreatedAt { get; set; }

        [Date]
        public DateTime? LastExecutedAt { get; set; }

        [Date]
        public DateTime? FinishedAt { get; set; }

        [Date]
        public DateTime? CancelledAt { get; set; }

        [Date]
        public DateTime? FaultedAt { get; set; }

        [Date]
        public DateTime? LastSavedAt { get; set; }

        public string GetId()
        {
            return Id;
        }
    }
}
