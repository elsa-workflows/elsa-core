using Elasticsearch.Net;
using Elsa.Elasticsearch.Options;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Runtime.Entities;
using Nest;

namespace Elsa.Elasticsearch.Common;

public static class Configuration
{
    public static ConnectionSettings SetupAuthentication(this ConnectionSettings settings, ElasticsearchOptions options)
    {
        if (!string.IsNullOrEmpty(options.ApiKey))
        {
            settings.ApiKeyAuthentication(new ApiKeyAuthenticationCredentials(options.ApiKey));
        }
        else if (!string.IsNullOrEmpty(options.Username) && !string.IsNullOrEmpty(options.Password))
        {
            settings.BasicAuthentication(options.Username, options.Password);
        }

        return settings;
    }
    
    public static ConnectionSettings SetupMappingsAndIndices(this ConnectionSettings settings)
    {
        settings.DefaultMappingFor<WorkflowInstance>(s => 
            s.IndexName(ElasticsearchOptions.Indices[typeof(WorkflowInstance)]));
        
        settings.DefaultMappingFor<WorkflowExecutionLogRecord>(s => 
            s.IndexName(ElasticsearchOptions.Indices[typeof(WorkflowExecutionLogRecord)]));
        
        return settings;
    }
}