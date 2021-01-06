using System.Threading;
using System.Threading.Tasks;

using AutoMapper;

using Elsa.Indexing.Models;

namespace Elsa.Indexing.Services
{
    public abstract class Indexer<TEntity, TElasticEntity>
        where TEntity : class
        where TElasticEntity : class, IElasticEntity
    {
        protected readonly ElasticsearchStore<TElasticEntity> elasticsearchStore;
        protected readonly IMapper mapper;

        public Indexer(ElasticsearchStore<TElasticEntity> elasticsearchStore, IMapper mapper)
        {
            this.elasticsearchStore = elasticsearchStore;
            this.mapper = mapper;
        }

        public Task DeleteAsync(string id, CancellationToken cancellationToken)
        {
            return elasticsearchStore.DeleteAsync(id, cancellationToken);
        }

        public Task IndexAsync(TEntity entity, CancellationToken cancellationToken)
        {
            var model = mapper.Map<TElasticEntity>(entity);
            return elasticsearchStore.SaveAsync(model, cancellationToken);
        }
    }
}
