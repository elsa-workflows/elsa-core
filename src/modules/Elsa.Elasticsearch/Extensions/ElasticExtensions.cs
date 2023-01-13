using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Elsa.Elasticsearch.Options;

namespace Elsa.Elasticsearch.Extensions;

internal static class ElasticExtensions
{
    public static ElasticsearchClientSettings ConfigureAuthentication(this ElasticsearchClientSettings settings, ElasticsearchOptions options)
    {
        if (!string.IsNullOrEmpty(options.ApiKey))
            settings.Authentication(new ApiKey(options.ApiKey));
        else if (!string.IsNullOrEmpty(options.Username) && !string.IsNullOrEmpty(options.Password))
            settings.Authentication(new BasicAuthentication(options.Username, options.Password));

        return settings;
    }
}