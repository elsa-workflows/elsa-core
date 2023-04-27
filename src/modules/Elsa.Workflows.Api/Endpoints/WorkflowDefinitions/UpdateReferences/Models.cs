namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.UpdateReferences;

public class Request
{
    public string DefinitionId { get; set; }
    public IEnumerable<string>? ConsumingWorkflowIds { get; set; }
}

public class Response
{
    public IEnumerable<string> AffectedWorkflows { get; set; }
}