using System;
using Elsa.Extensions;
using Elsa.Indexing.Models;
using Elsa.Indexing.Profiles;
using Elsa.Indexing.Services;
using Elsa.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Indexing.Extensions
{
    public static class ElsaIndexingOptionsExtensions
    {
        public static ElsaIndexingOptions UseElasticsearch(this ElsaIndexingOptions options, Action<ElsaElasticsearchOptions> configure)
        {
            options.Services.Configure(configure);

            options.ContainerBuilder
                .AddScoped<IWorkflowDefinitionSearch, WorkflowDefinitionSearch>()
                .AddScoped<IWorkflowInstanceSearch, WorkflowInstanceSearch>()
                .AddScoped<IWorkflowDefinitionIndexer, WorkflowDefinitionIndexer>()
                .AddScoped<IWorkflowInstanceIndexer, WorkflowInstanceIndexer>()
                .AddMultiton<ElasticsearchContext>()
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
                .AddStartupTask<ElasticsearchInitializer>();

            options.Services.AddAutoMapperProfile<ElasticsearchProfile>();

            return options;
        }
    }
}
