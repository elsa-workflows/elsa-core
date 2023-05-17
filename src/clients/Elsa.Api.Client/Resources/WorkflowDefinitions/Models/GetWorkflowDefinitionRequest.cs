namespace Elsa.Api.Client.Resources.WorkflowDefinitions.Models;

public class GetWorkflowDefinitionRequest
{
    public string DefinitionId { get; set; } = default!;
    public string? VersionOptions { get; set; }
}