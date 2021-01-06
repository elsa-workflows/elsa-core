using System;
using System.Threading;
using System.Threading.Tasks;

using Elsa.Indexing.Models;

using Nest;

namespace Elsa.Indexing.Services
{
    public class ElasticsearchStore<TEntity> where TEntity : class, IElasticEntity
    {
        protected readonly ElasticsearchContext context;
        protected readonly string indexName;

        public ElasticsearchStore(ElasticsearchContext context, string indexName)
        {
            this.context = context;
            this.indexName = indexName;
        }

        protected IElasticClient Client => context.Client;

        public Task<CreateIndexResponse> CreateIndexAsync(Func<CreateIndexDescriptor, ICreateIndexRequest> selector) 
        {
            return Client.Indices.CreateAsync(indexName, selector);
        }

        public Task<CreateIndexResponse> CreateIndexAsync()
        {
            return Client.Indices.CreateAsync(indexName, c => c
                .Map<TEntity>(m => m
                    .AutoMap(3)
                    ));
        }

        public async Task<bool> ExistsIndexAsync()
        {
            return (await Client.Indices.ExistsAsync(indexName)).Exists;
        }

        public Task<DeleteResponse> DeleteAsync(string id, CancellationToken cancellationToken)
        {
            return Client.DeleteAsync(DocumentPath<TEntity>.Id(id).Index(indexName), null, cancellationToken);
        }

        public Task<UpdateResponse<TEntity>> SaveAsync(TEntity document, CancellationToken cancellationToken) 
        {
            return Client.UpdateAsync(DocumentPath<TEntity>.Id(document.GetId()), 
                u => u.Index(indexName).Doc(document).DocAsUpsert(true),
                cancellationToken);
        }
    }
}
