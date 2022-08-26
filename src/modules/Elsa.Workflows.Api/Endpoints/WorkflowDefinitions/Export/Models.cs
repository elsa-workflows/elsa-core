namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Export;

public class Request
{
    public string DefinitionId { get; set; } = default!;
    public string? VersionOptions { get; set; }
}