using System.Threading;
using System.Threading.Tasks;

using AutoMapper;

using Elsa.Indexing.Models;
using Elsa.Models;

namespace Elsa.Indexing.Services
{
    public class WorkflowDefinitionIndexer : Indexer<WorkflowDefinition, ElasticWorkflowDefinition>, IWorkflowDefinitionIndexer
    {
        public WorkflowDefinitionIndexer(ElasticsearchStore<ElasticWorkflowDefinition> elasticsearchStore, IMapper mapper) : base(elasticsearchStore, mapper)
        {
        }

        public Task DeleteAsync(WorkflowDefinition definition, CancellationToken cancellationToken)
        {
            return DeleteAsync(definition.Id, cancellationToken);
        }
    }
}
