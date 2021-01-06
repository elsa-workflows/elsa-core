using Elasticsearch.Net;
using Microsoft.Extensions.Options;
using Nest;
using System;
using System.Collections.Generic;
using System.Text;

namespace Elsa.Indexing.Services
{
    public class ElasticsearchStore
    {
        private readonly ElasticClient _elasticClient;

        public ElasticsearchStore(IOptions<ElsaElasticsearchOptions> options)
        {
            var pool = new StaticConnectionPool(options.Value.Uri);
            var settings = new ConnectionSettings(pool);

            if(options.Value.BasicAuthentication != null)
            {
                settings.BasicAuthentication(options.Value.BasicAuthentication.Username, options.Value.BasicAuthentication.Password);
            } else if(options.Value.ApiKeyAuthentication != null)
            {
                settings.ApiKeyAuthentication(options.Value.ApiKeyAuthentication.Id, options.Value.ApiKeyAuthentication.ApiKey);
            }
           
            _elasticClient = new ElasticClient(settings);
        }

    }
}
