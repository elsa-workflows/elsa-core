using System.Collections.ObjectModel;
using Elsa.Common.Models;
using Microsoft.Extensions.Logging;
using Nest;

namespace Elsa.Elasticsearch.Common;

public class ElasticStore<T> where T : class
{
    private readonly ElasticClient _elasticClient;
    private readonly ILogger _logger;

    public ElasticStore(ElasticClient elasticClient, ILogger<ElasticStore<T>> logger)
    {
        _elasticClient = elasticClient;
        _logger = logger;
    }

    public async Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        var response = await _elasticClient.GetAsync(DocumentPath<T>.Id(id), ct: cancellationToken);

        if (response.ApiCall.Success) return response.Source;
        
        _logger.LogError("Failed to fetch data from Elasticsearch: {message}", response.ServerError?.ToString());
        return null;
    }

    public async Task<Page<T>> SearchAsync(Func<QueryContainerDescriptor<T>, QueryContainer> query, PageArgs? pageArgs, CancellationToken cancellationToken)
    {
        var search = new SearchDescriptor<T>().Query(query);
        if (pageArgs != default)
        {
            search = search.From(pageArgs.Offset).Size(pageArgs.Limit);
        }

        var response = await _elasticClient.SearchAsync<T>(search, cancellationToken);
        
        if (response.ApiCall.Success) 
            return new Page<T>(response.Hits.Select(hit => hit.Source).ToList(), response.Total);
        
        _logger.LogError("Failed to search data in Elasticsearch: {message}", response.ServerError?.ToString());
        return new Page<T>(new Collection<T>(), 0);
    }
    
    public async Task<Page<T>> SearchAsync(SearchDescriptor<T> search, PageArgs? pageArgs, CancellationToken cancellationToken)
    {
        if (pageArgs != default)
        {
            search = search.From(pageArgs.Offset).Size(pageArgs.Limit);
        }

        var response = await _elasticClient.SearchAsync<T>(search, cancellationToken);
        
        if (response.ApiCall.Success) 
            return new Page<T>(response.Hits.Select(hit => hit.Source).ToList(), response.Total);
        
        _logger.LogError("Failed to search data in Elasticsearch: {message}", response.ServerError?.ToString());
        return new Page<T>(new Collection<T>(), 0);
    }

    public async Task<bool> SaveAsync(T model, CancellationToken cancellationToken)
    {
        var response = await _elasticClient.IndexAsync(model, descriptor => descriptor, cancellationToken);

        if (response.ApiCall.Success) return true;
        
        _logger.LogError("Failed to save data in Elasticsearch: {message}", response.ServerError?.ToString());
        return false;
    }

    public async Task<bool> SaveManyAsync(IEnumerable<T> documents, CancellationToken cancellationToken)
    {
        var response = await _elasticClient.IndexManyAsync(documents, cancellationToken: cancellationToken);

        if (response.ApiCall.Success) return true;
        
        _logger.LogError("Failed to save data in Elasticsearch: {message}", response.ServerError?.ToString());
        return false;
    }

    public async Task<bool> DeleteByIdAsync(string id, CancellationToken cancellationToken)
    {
        var response = await _elasticClient.DeleteAsync(DocumentPath<T>.Id(id), ct: cancellationToken);

        if (response.ApiCall.Success) return true;
        
        _logger.LogError("Failed to delete data in Elasticsearch: {message}", response.ServerError?.ToString());
        return false;
    }
    
    public async Task<int> DeleteManyAsync(IEnumerable<T> list, CancellationToken cancellationToken)
    {
        var response = await _elasticClient.DeleteManyAsync(list, cancellationToken: cancellationToken);

        if (response.ApiCall.Success) return 0;
        
        _logger.LogError("Failed to delete data in Elasticsearch: {message}", response.ServerError?.ToString());
        return response.Items.Count;
    }

    public async Task<bool> DeleteByQueryAsync(Func<QueryContainerDescriptor<T>, QueryContainer> query, CancellationToken cancellationToken)
    {
        var response = await _elasticClient.DeleteByQueryAsync<T>(q => q
            .Query(query), cancellationToken);

        if (response.ApiCall.Success) return true;
        
        _logger.LogError("Failed to delete data in Elasticsearch: {message}", response.ServerError?.ToString());
        return false;
    }
    
    public async Task<long> CountAsync(Func<QueryContainerDescriptor<T>, QueryContainer> query, CancellationToken cancellationToken)
    {
        var response = await _elasticClient.CountAsync<T>(s =>
            s.Query(query), cancellationToken);
        
        if (response.ApiCall.Success) return response.Count;
        
        _logger.LogError("Failed to count data in Elasticsearch: {message}", response.ServerError?.ToString());
        return 0;
    }
}