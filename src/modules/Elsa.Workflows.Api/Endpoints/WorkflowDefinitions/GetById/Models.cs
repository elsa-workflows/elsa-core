namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.GetById;

internal class Request
{
    public string Id { get; set; } = default!;
    
    /// <summary>
    /// True if the response should include the root activity of composite activities.
    /// </summary>
    public bool IncludeCompositeRoot { get; set; }
}