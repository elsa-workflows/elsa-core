using Elastic.Clients.Elasticsearch;
using Elsa.Elasticsearch.Services;

namespace Elsa.Elasticsearch.Implementations.RolloverStrategies;

/// <summary>
/// Generates new index names based on the current year and month.
/// </summary>
public class MonthlyRollover : IIndexRolloverStrategy
{
    private readonly ElasticsearchClient _client;

    /// <summary>
    /// Constructor.
    /// </summary>
    public MonthlyRollover(ElasticsearchClient client)
    {
        _client = client;
    }

    /// <inheritdoc />
    public async Task ApplyAsync(CancellationToken cancellationToken)
    {
        var getAliasResponse = await _client.Indices.GetAliasAsync(cancellationToken);
        var aliases = getAliasResponse.Aliases.Values.SelectMany(s => s.Aliases.Select(a => a.Key));

        foreach (var alias in aliases)
        {
            var newIndexName = GenerateName(alias);
            var indexExists = (await _client.Indices.ExistsAsync(newIndexName, cancellationToken)).Exists;

            if (indexExists)
                continue;

            await _client.Indices.RolloverAsync(alias, cfg => cfg.NewIndex(newIndexName), cancellationToken);
        }
    }

    private string GenerateName(string aliasName)
    {
        var now = DateTime.Now;
        var month = now.ToString("MM");
        var year = now.Year;

        return aliasName + "-" + year + "-" + month;
    }
}