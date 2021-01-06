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

        [Text]
        public string? Name { get; set; }

        [Text]
        public string? DisplayName { get; set; }

        [Text]
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

        public string GetId()
        {
            return Id;
        }
    }
}
