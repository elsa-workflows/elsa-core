using System.Collections.Generic;

using Nest;

namespace Elsa.Indexing.Models
{
    [ElasticsearchType(IdProperty = nameof(Id))]
    public class ElasticWorkflowDefinition : IElasticEntity
    {
        [Keyword]
        public string Id { get; set; } = default!;

        [Keyword]
        public string DefinitionVersionId { get; set; } = default!;

        [Keyword]
        public string? TenantId { get; set; }

        [Text(Analyzer = ElsaElasticsearchConsts.Analyzer, SearchAnalyzer = ElsaElasticsearchConsts.SearchAnalyzer)]
        public string? Name { get; set; }

        [Text(Analyzer = ElsaElasticsearchConsts.Analyzer, SearchAnalyzer = ElsaElasticsearchConsts.SearchAnalyzer)]
        public string? DisplayName { get; set; }

        [Text(Analyzer = ElsaElasticsearchConsts.Analyzer, SearchAnalyzer = ElsaElasticsearchConsts.SearchAnalyzer)]
        public string? Description { get; set; }

        [Number]
        public int Version { get; set; }

        [Boolean]
        public bool IsSingleton { get; set; }

        [Boolean]
        public bool IsEnabled { get; set; }

        [Boolean]
        public bool IsPublished { get; set; }

        [Boolean]
        public bool IsLatest { get; set; }

        [Nested]
        public List<ElasticActivityDefinition> Activities { get; set; } = new List<ElasticActivityDefinition>();

        public string GetId()
        {
            return Id;
        }
    }
}
