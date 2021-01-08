using Elsa.Indexing.Models;
using Elsa.Indexing.Profiles;
using Elsa.Indexing.Services;
using Elsa.Runtime;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using System;

namespace Elsa.Indexing.Extensions
{
    public static class ElsaIndexingOptionsExtensions
    {
        public static ElsaIndexingOptions UseElasticsearch(this ElsaIndexingOptions options, Action<ElsaElasticsearchOptions> configure)
        {
            options.Services.Configure(configure);

            options.Services
                .AddScoped<IWorkflowDefinitionSearch, WorkflowDefinitionSearch>()
                .AddScoped<IWorkflowInstanceSearch, WorkflowInstanceSearch>()
                .AddScoped<IWorkflowDefinitionIndexer, WorkflowDefinitionIndexer>()
                .AddScoped<IWorkflowInstanceIndexer, WorkflowInstanceIndexer>()
                .AddSingleton<ElasticsearchContext>()
                .AddScoped(services =>
                {
                    var context = services.GetRequiredService<ElasticsearchContext>();
                    var indexName = services.GetRequiredService<IOptions<ElsaElasticsearchOptions>>().Value.WorkflowInstanceIndexName;

                    return new ElasticsearchStore<ElasticWorkflowInstance>(context, indexName);
                })
                .AddScoped(services =>
                {
                     var context = services.GetRequiredService<ElasticsearchContext>();
                     var indexName = services.GetRequiredService<IOptions<ElsaElasticsearchOptions>>().Value.WorkflowDefinitionIndexName;

                     return new ElasticsearchStore<ElasticWorkflowDefinition>(context, indexName);
                })
                .AddAutoMapperProfile<ElasticsearchProfile>()
                .AddStartupTask<ElasticsearchInitializer>();

            return options;
        }
    }
}
