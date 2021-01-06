using Elsa.Indexing.Profiles;
using Elsa.Indexing.Services;
using Microsoft.Extensions.DependencyInjection;
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
                .AddScoped<ElasticsearchStore>();

            options.Services.AddAutoMapperProfile<ElasticsearchProfile>();

            return options;
        }
    }
}
