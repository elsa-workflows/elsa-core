using System.Threading;
using System.Threading.Tasks;

using AutoMapper;

using Elsa.Indexing.Models;
using Elsa.Models;

namespace Elsa.Indexing.Services
{
    public class WorkflowInstanceIndexer : Indexer<WorkflowInstance, ElasticWorkflowInstance>, IWorkflowInstanceIndexer
    {
        public WorkflowInstanceIndexer(ElasticsearchStore<ElasticWorkflowInstance> elasticsearchStore, IMapper mapper) : base(elasticsearchStore, mapper)
        {
        }

        public Task DeleteAsync(WorkflowInstance instance, CancellationToken cancellationToken)
        {
            return DeleteAsync(instance.Id, cancellationToken);
        }     
    }
}
