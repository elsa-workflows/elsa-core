using System.Threading;
using System.Threading.Tasks;

using Elsa.Indexing.Models;
using Elsa.Indexing.Services;
using Elsa.Services;

using Microsoft.Extensions.Options;

namespace Elsa.Indexing
{
    public class ElasticsearchInitialiser : IStartupTask
    {
        ElasticsearchStore<ElasticWorkflowDefinition> _elasticsearchDefinitionStore;
        ElasticsearchStore<ElasticWorkflowInstance> _elasticsearchInstanceStore;

        public ElasticsearchInitialiser(ElasticsearchStore<ElasticWorkflowDefinition> elasticsearchDefinitionStore, ElasticsearchStore<ElasticWorkflowInstance> elasticsearchInstanceStore)
        {
            _elasticsearchDefinitionStore = elasticsearchDefinitionStore;
            _elasticsearchInstanceStore = elasticsearchInstanceStore;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            if(await _elasticsearchDefinitionStore.ExistsIndexAsync() == false) 
            {
                await _elasticsearchDefinitionStore.CreateIndexAsync();
            }

            if (await _elasticsearchInstanceStore.ExistsIndexAsync() == false)
            {
                await _elasticsearchInstanceStore.CreateIndexAsync();
            }
        }
    }
}
