using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Elasticsearch.Options;

public class ElasticsearchOptions
{
    public const string Elasticsearch = "Elasticsearch";

    public static readonly Dictionary<Type, string> Indices = new()
    {
        {typeof(WorkflowInstance),"workflow-instances"},
        {typeof(WorkflowExecutionLogRecord),"execution-log-records"}
    };

    public string Endpoint { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public string ApiKey { get; set; }
}