namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Consumers;

internal record Request
{
    /// <summary>
    /// The workflow definition ID.
    /// </summary>
    public string DefinitionId { get; set; } = null!;
}

internal record Response(ICollection<string> ConsumingWorkflowDefinitionIds);
