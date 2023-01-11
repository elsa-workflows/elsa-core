using System.Collections.ObjectModel;
using Elastic.Clients.Elasticsearch;
using Elsa.Common.Models;
using Microsoft.Extensions.Logging;
using Exception = System.Exception;

namespace Elsa.Elasticsearch.Common;

public class ElasticStore<T> where T : class
{
    private readonly ElasticsearchClient _elasticClient;
    private readonly ILogger _logger;

    public ElasticStore(ElasticsearchClient elasticClient, ILogger<ElasticStore<T>> logger)
    {
        _elasticClient = elasticClient;
        _logger = logger;
    }

    public async Task<Page<T>> SearchAsync(Action<SearchRequestDescriptor<T>> search, PageArgs? pageArgs, CancellationToken cancellationToken)
    {
        if (pageArgs != default)
        {
            search += s => s.From(pageArgs.Offset).Size(pageArgs.Limit);
        }

        var response = await _elasticClient.SearchAsync(search, cancellationToken);
        
        if (response.IsSuccess())
            return new Page<T>(response.Hits.Select(hit => hit.Source).ToList()!, response.Total);
        
        _logger.LogError("Failed to search data in Elasticsearch: {message}", response.ElasticsearchServerError.ToString());
        return new Page<T>(new Collection<T>(), 0);
    }

    public async Task SaveAsync(T model, CancellationToken cancellationToken)
    {
        var response = await _elasticClient.IndexAsync(model, cancellationToken);

        if (response.IsSuccess()) return;
        
        throw new Exception($"Failed to save data in Elasticsearch: {response.ElasticsearchServerError}");
    }

    public async Task SaveManyAsync(IEnumerable<T> documents, CancellationToken cancellationToken)
    {
        var response = await _elasticClient.IndexManyAsync(documents, cancellationToken);

        if (response.IsSuccess()) return;
        
        throw new Exception($"Failed to save data in Elasticsearch: {response.ElasticsearchServerError}");
    }

    public async Task<long> DeleteManyAsync(IEnumerable<T> list, CancellationToken cancellationToken)
    {
        var response = await _elasticClient.BulkAsync(b => b.DeleteMany(list), cancellationToken);
        
        if (response.IsSuccess()) return response.Items.Count(i => i.IsValid);
        
        _logger.LogError("Failed to bulk delete data in Elasticsearch: {message}", response.ElasticsearchServerError.ToString());
        return 0;
    }

    public async Task<long> DeleteByQueryAsync(Action<DeleteByQueryRequestDescriptor<T>> query, CancellationToken cancellationToken)
    {
        var response = await _elasticClient.DeleteByQueryAsync(Indices.All, query, cancellationToken);

        if (response.IsSuccess()) return response.Deleted ?? 0;
        
        _logger.LogError("Failed to delete data in Elasticsearch: {message}", response.ElasticsearchServerError.ToString());
        return 0;
    }
}