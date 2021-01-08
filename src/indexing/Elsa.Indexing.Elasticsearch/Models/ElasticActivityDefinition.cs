using Nest;

namespace Elsa.Indexing.Models
{
    public class ElasticActivityDefinition
    {
        [Keyword]
        public string ActivityId { get; set; } = default!;

        [Text(Analyzer = ElsaElasticsearchConsts.Analyzer, SearchAnalyzer = ElsaElasticsearchConsts.SearchAnalyzer)]
        public string? Name { get; set; }

        [Text(Analyzer = ElsaElasticsearchConsts.Analyzer, SearchAnalyzer = ElsaElasticsearchConsts.SearchAnalyzer)]
        public string? DisplayName { get; set; }

        [Text(Analyzer = ElsaElasticsearchConsts.Analyzer, SearchAnalyzer = ElsaElasticsearchConsts.SearchAnalyzer)]
        public string? Description { get; set; }
    }
}
