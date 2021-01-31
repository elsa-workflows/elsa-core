using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using AutoMapper;

using Elsa.Indexing.Models;

using Nest;

namespace Elsa.Indexing.Services
{

    public abstract class Searcher<TEntity> where TEntity : class, IElasticEntity
    {
        protected readonly ElasticsearchContext elasticsearchContext;
        protected readonly IMapper mapper;

        protected Searcher(ElasticsearchContext elasticsearchContext, IMapper mapper)
        {
            this.elasticsearchContext = elasticsearchContext;
            this.mapper = mapper;
        }

        protected IElasticClient Client => elasticsearchContext.Client;

        protected async Task<List<TModel>> SearchAsync<TModel>(string indexName,
            List<Func<QueryContainerDescriptor<TEntity>, QueryContainer>> filters,
            List<Func<QueryContainerDescriptor<TEntity>, QueryContainer>> shouldFilters,
            int from,
            int take,
            int minimumShouldMatches = 1,
            CancellationToken cancellationToken = default)
        {
            var result = await elasticsearchContext.Client.SearchAsync<TEntity>(r => r
                .Index(indexName)
                .TrackScores(true)
                .From(from)
                .Take(take)
                .Sort(s => s
                       .Field("_score", SortOrder.Descending)
                )
                .Query(q => q
                    .Bool(b => b
                        .Filter(filters)
                        .Should(shouldFilters)
                        .MinimumShouldMatch(1)
                    )
                )
             );

            return mapper.Map<List<TModel>>(result.Documents);
        }

        protected List<Func<QueryContainerDescriptor<TEntity>, QueryContainer>> GetEmptyQuery() => new List<Func<QueryContainerDescriptor<TEntity>, QueryContainer>>();
    }
}
