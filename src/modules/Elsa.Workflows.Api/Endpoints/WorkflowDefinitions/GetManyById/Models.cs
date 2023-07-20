namespace Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.GetManyById;

internal class Request
{
    public ICollection<string> Ids { get; set; } = default!;
    
    /// <summary>
    /// True if the response should include the root activity of composite activities.
    /// </summary>
    public bool IncludeCompositeRoot { get; set; }
}