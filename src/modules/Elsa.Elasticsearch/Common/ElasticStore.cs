using System.Collections.ObjectModel;
using Elastic.Clients.Elasticsearch;
using Elsa.Common.Models;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Exception = System.Exception;

namespace Elsa.Elasticsearch.Common;

/// <summary>
/// A thin wrapper around <see cref="ElasticsearchClient"/> for easy re-usability.
/// </summary>
/// <typeparam name="T">The document type.</typeparam>
[PublicAPI]
public class ElasticStore<T> where T : class
{
    private readonly ElasticsearchClient _elasticClient;
    private readonly ILogger _logger;

    /// <summary>
    /// Constructor.
    /// </summary>
    public ElasticStore(ElasticsearchClient elasticClient, ILogger<ElasticStore<T>> logger)
    {
        _elasticClient = elasticClient;
        _logger = logger;
    }

    /// <summary>
    /// Searches the index using the specified search descriptor.
    /// </summary>
    public async Task<Page<T>> SearchAsync(Action<SearchRequestDescriptor<T>> search, PageArgs? pageArgs, CancellationToken cancellationToken)
    {
        if (pageArgs?.Page != null && pageArgs?.PageSize != null)
        {
            search += s => s.From(pageArgs.Offset).Size(pageArgs.Limit);
        }

        var response = await _elasticClient.SearchAsync(search, cancellationToken);

        if (!response.IsSuccess())
            throw new Exception(response.DebugInformation);
            
        return new Page<T>(response.Hits.Select(hit => hit.Source).ToList()!, response.Total);
    }

    /// <summary>
    /// Stores the specified document in the index.
    /// </summary>
    public async Task SaveAsync(T document, CancellationToken cancellationToken)
    {
        var response = await _elasticClient.IndexAsync(document, cancellationToken);

        if (!response.IsSuccess()) 
            throw new Exception($"Failed to save data in Elasticsearch: {response.ElasticsearchServerError}");
    }

    /// <summary>
    /// Stores the specified documents in the index.
    /// </summary>
    public async Task SaveManyAsync(IEnumerable<T> documents, CancellationToken cancellationToken)
    {
        var response = await _elasticClient.IndexManyAsync(documents, cancellationToken);

        if (!response.IsSuccess()) 
            throw new Exception($"Failed to save data in Elasticsearch: {response.ElasticsearchServerError}");
    }

    /// <summary>
    /// Deletes the specified set of documents from the index.
    /// </summary>
    public async Task<long> DeleteManyAsync(IEnumerable<T> documents, CancellationToken cancellationToken)
    {
        var response = await _elasticClient.BulkAsync(b => b.DeleteMany(documents), cancellationToken);

        if (!response.IsSuccess())
            throw new Exception(response.DebugInformation);

        return response.Items.Count(i => i.IsValid);
    }

    /// <summary>
    /// Deletes the documents matching the specified query.
    /// </summary>
    public async Task<long> DeleteByQueryAsync(Action<DeleteByQueryRequestDescriptor<T>> query, CancellationToken cancellationToken)
    {
        var response = await _elasticClient.DeleteByQueryAsync(Indices.All, query, cancellationToken);
        
        if (!response.IsSuccess())
            throw new Exception(response.DebugInformation);

        return response.Deleted ?? 0;
    }
}