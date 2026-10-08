namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.GetByDefinitionId;

internal class Request
{
    public string DefinitionId { get; set; } = default!;
    public string? VersionOptions { get; set; }
}