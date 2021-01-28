using System;
using System.Threading;
using System.Threading.Tasks;

using Elsa.Indexing.Models;
using Elsa.Indexing.Services;
using Elsa.Services;

using Nest;

namespace Elsa.Indexing
{
    public class ElasticsearchInitializer : IStartupTask
    {
        readonly ElasticsearchStore<ElasticWorkflowDefinition> _elasticsearchDefinitionStore;
        readonly ElasticsearchStore<ElasticWorkflowInstance> _elasticsearchInstanceStore;

        public ElasticsearchInitializer(ElasticsearchStore<ElasticWorkflowDefinition> elasticsearchDefinitionStore, ElasticsearchStore<ElasticWorkflowInstance> elasticsearchInstanceStore)
        {
            _elasticsearchDefinitionStore = elasticsearchDefinitionStore;
            _elasticsearchInstanceStore = elasticsearchInstanceStore;
        }

        public int Order => 1000;

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            if(await _elasticsearchDefinitionStore.ExistsIndexAsync() == false) 
            {
                await _elasticsearchDefinitionStore.CreateIndexAsync(CreateIndexSettings<ElasticWorkflowDefinition>());
            }

            if (await _elasticsearchInstanceStore.ExistsIndexAsync() == false)
            {
                await _elasticsearchInstanceStore.CreateIndexAsync(CreateIndexSettings<ElasticWorkflowDefinition>());
            }
        }

        private Func<CreateIndexDescriptor, ICreateIndexRequest> CreateIndexSettings<T>() where T : class, IElasticEntity
        {
            return c => c
                    .Map<T>(m => m
                        .AutoMap(3)
                    )
                    .Settings(s => s
                        .Analysis(a => a
                            .Analyzers(an => an
                                .Custom(ElsaElasticsearchConsts.SearchAnalyzer, c => c
                                    .Filters("lowercase", "shingle_filter")
                                    .Tokenizer("standard")
                                )
                            )
                            .TokenFilters(f => f
                                .Shingle("shingle_filter", s => s
                                     .MinShingleSize(2)
                                     .MaxShingleSize(4)
                                )
                            )
                            .Normalizers(n => n
                                .Custom(ElsaElasticsearchConsts.Normalizer, c => c
                                    .Filters("lowercase")
                                )
                            )
                        )
                    );
        }
    }
}
