namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Execute;

public class Request
{
    public string DefinitionId { get; set; } = default!;
    public string? CorrelationId { get; set; }
}

public class Response
{
    public Response(string instanceId)
    {
        InstanceId = instanceId;
    }
    
    public string InstanceId { get; }
}