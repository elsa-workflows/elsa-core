namespace Elsa.Api.Client.Resources.WorkflowDefinitions.Requests;

/// <summary>
/// Represents a request to get a workflow definition.
/// </summary>
public class GetWorkflowDefinitionRequest
{
    /// <summary>
    /// Gets or sets a value indicating whether the response should include the root activity of composite activities.
    /// </summary>
    public bool IncludeCompositeRoot { get; set; }
}