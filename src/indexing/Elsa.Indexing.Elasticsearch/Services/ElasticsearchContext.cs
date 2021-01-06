using Elasticsearch.Net;
using Microsoft.Extensions.Options;
using Nest;

namespace Elsa.Indexing.Services
{
    public class ElasticsearchContext
    {    
        public ElasticsearchContext(IOptions<ElsaElasticsearchOptions> options)
        {
            var pool = new SniffingConnectionPool(options.Value.Uri);
            var settings = new ConnectionSettings(pool);

            if(options.Value.BasicAuthentication != null)
            {
                settings.BasicAuthentication(options.Value.BasicAuthentication.Username, options.Value.BasicAuthentication.Password);
            } else if(options.Value.ApiKeyAuthentication != null)
            {
                settings.ApiKeyAuthentication(options.Value.ApiKeyAuthentication.Id, options.Value.ApiKeyAuthentication.ApiKey);
            }
           
            Client = new ElasticClient(settings);
        }

        public IElasticClient Client { get; }

    }
}
