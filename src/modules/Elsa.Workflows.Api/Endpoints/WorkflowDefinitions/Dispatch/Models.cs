namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Dispatch;

public class Request
{
    public string DefinitionId { get; set; } = default!;
    public string? CorrelationId { get; set; }
}

public class Response
{
}