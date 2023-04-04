namespace Elsa.Api.Client.Contracts;

public class GetWorkflowDefinitionRequest
{
    public string DefinitionId { get; set; } = default!;
    public string? VersionOptions { get; set; }
}