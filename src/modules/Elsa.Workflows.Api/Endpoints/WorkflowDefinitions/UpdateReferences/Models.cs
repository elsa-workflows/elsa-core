namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.UpdateReferences;

internal record Request
{
    public string DefinitionId { get; set; } = default!;
}

internal record Response(IEnumerable<string> AffectedWorkflows);