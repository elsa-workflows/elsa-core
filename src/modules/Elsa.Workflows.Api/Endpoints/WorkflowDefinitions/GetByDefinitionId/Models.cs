namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.GetByDefinitionId;

internal class Request
{
    public string DefinitionId { get; set; } = default!;
    public string? VersionOptions { get; set; }
    
    /// <summary>
    /// True if the response should include the root activity of composite activities.
    /// </summary>
    public bool IncludeCompositeRoot { get; set; }
}