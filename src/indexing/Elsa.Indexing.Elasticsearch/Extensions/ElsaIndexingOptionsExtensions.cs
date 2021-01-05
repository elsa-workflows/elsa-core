using Elsa.Indexing.Profiles;
using Elsa.Indexing.Services;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Elsa.Indexing
{
    public static class ElsaIndexingOptionsExtensions
    {
        public static ElsaOptions UseElasticsearch(this ElsaOptions options, Action<ElsaElasticsearchOptions> configure)
        {
            options.Services.Configure(configure);

            options.Services.AddScoped<IWorkflowDefinitionSearch, WorkflowDefinitionSearch>();
            options.Services.AddScoped<IWorkflowInstanceSearch, WorkflowInstanceSearch>();
            options.Services.AddScoped<ElasticsearchStore>();

            options.Services.AddAutoMapperProfile<ElasticsearchProfile>();

            return options;
        }
    }
}
