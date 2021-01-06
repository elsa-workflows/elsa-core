using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using AutoMapper;

using Elsa.Indexing.Extensions;
using Elsa.Indexing.Models;

using Microsoft.Extensions.Options;

using Nest;

namespace Elsa.Indexing.Services
{
    public class WorkflowDefinitionSearch : Searcher<ElasticWorkflowDefinition>, IWorkflowDefinitionSearch
    {
        private readonly string _indexName;

        public WorkflowDefinitionSearch(ElasticsearchContext elasticsearchContext, IMapper mapper, IOptions<ElsaElasticsearchOptions> options) : base(elasticsearchContext, mapper)
        {
            _indexName = options.Value.WorkflowDefinitionIndexName;
        }

        public Task<List<WorkflowDefinitionIndexModel>> SearchAsync(string search,
            string? tenantId = null,
            bool? isEnabled = null,
            int from = 0,
            int take = 20,
            CancellationToken cancellationToken = default)
        {
            var filters = GetEmptyQuery();
            var shouldFilters = GetEmptyQuery();

            filters.AddWhenNotEmpty(f => f.Term(t => t.TenantId, tenantId), tenantId);
            filters.AddWhenNotEmpty(f => f.Term(t => t.IsEnabled, isEnabled), isEnabled);

            shouldFilters.Add(f => f.Match(fu => fu
                   .Field(fi => fi.Name)
                   .Query(search)
                   .Fuzziness(Fuzziness.Auto)));

            shouldFilters.Add(f => f.Match(fu => fu
                   .Field(fi => fi.Description)
                   .Query(search)
                   .Fuzziness(Fuzziness.Auto)));

            shouldFilters.Add(f => f.Match(fu => fu
                   .Field(fi => fi.DisplayName)
                   .Query(search)
                   .Fuzziness(Fuzziness.Auto)));

            shouldFilters.Add(f => f.Nested(n => n
                  .Path(p => p.Activities)
                  .Query(q => q
                        .Bool(b => b
                            .MinimumShouldMatch(1)
                            .Should(s => s
                                    .Match(fu => fu                            
                                       .Field(fi => fi.Activities.First().DisplayName)
                                       .Query(search)
                                       .Fuzziness(Fuzziness.Auto)),
                                   s => s
                                    .Match(fu => fu
                                            .Field(fi => fi.Activities.First().Description)
                                            .Query(search)
                                            .Fuzziness(Fuzziness.Auto)),
                                    s => s
                                    .Match(fu => fu
                                            .Field(fi => fi.Activities.First().ActivityId)
                                            .Query(search)
                                            .Fuzziness(Fuzziness.Auto)))
                            ))
                        ));

            return SearchAsync<WorkflowDefinitionIndexModel>(_indexName, filters, shouldFilters, from, take, 1, cancellationToken);
        }
    }
}
