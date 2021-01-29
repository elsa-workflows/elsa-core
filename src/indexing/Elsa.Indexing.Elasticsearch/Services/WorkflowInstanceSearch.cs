using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using AutoMapper;
using Elsa.Indexing.Extensions;
using Elsa.Indexing.Models;
using Elsa.Models;

using Microsoft.Extensions.Options;

using Nest;

namespace Elsa.Indexing.Services
{
    public class WorkflowInstanceSearch : Searcher<ElasticWorkflowInstance>, IWorkflowInstanceSearch
    {
        private readonly string _indexName;

        public WorkflowInstanceSearch(ElasticsearchContext elasticsearchContext, IMapper mapper, IOptions<ElsaElasticsearchOptions> options) : base(elasticsearchContext, mapper)
        {
            _indexName = options.Value.WorkflowInstanceIndexName;
        }

        public Task<List<WorkflowInstanceIndexModel>> SearchAsync(string search, 
            string? contextType = null, 
            string? contextId = null, 
            string? definitionId = null, 
            string? tenantId = null, 
            WorkflowStatus? workflowStatus = null, 
            string? correlationId = null,
            int from = 0,
            int take = 20,
            CancellationToken cancellationToken = default)
        {
            var filters = GetEmptyQuery();
            var shouldFilters = GetEmptyQuery();

            if (contextType != null)
            {
                filters.Add(f => f.Term(t => t.ContextType, contextType));

                filters.AddWhenNotEmpty(f => f.Term(t => t.ContextId, contextId), contextId);
            }

            filters.AddWhenNotEmpty(f => f.Term(t => t.DefinitionId, definitionId), definitionId);
            filters.AddWhenNotEmpty(f => f.Term(t => t.TenantId, tenantId), tenantId);
            filters.AddWhenNotEmpty(f => f.Term(t => t.WorkflowStatus, workflowStatus), workflowStatus);
            filters.AddWhenNotEmpty(f => f.Term(t => t.CorrelationId, correlationId), correlationId);

            shouldFilters.Add(f => f.Match(fu => fu
                    .Field(fi => fi.Name)
                    .Query(search)
                    .Fuzziness(Fuzziness.Auto)));

            shouldFilters.Add(f => f.Match(fu => fu
                  .Field(fi => fi.DefinitionId)
                  .Query(search)
                  .Fuzziness(Fuzziness.Auto)));

            return SearchAsync<WorkflowInstanceIndexModel>(_indexName, filters, shouldFilters, from, take, 1, cancellationToken);
        }

        public Task<List<WorkflowInstanceIndexModel>> SearchAsync<TContext>(string search, 
            string? contextId, 
            string? definitionId = null, 
            string? tenantId = null, 
            WorkflowStatus? workflowStatus = null, 
            string? correlationId = null,
            int from = 0,
            int take = 20,
            CancellationToken cancellationToken = default)
        {
            return SearchAsync(search, typeof(TContext).GetContextTypeName(), contextId, definitionId, tenantId, workflowStatus, correlationId, from, take, cancellationToken);
        }
    }
}
