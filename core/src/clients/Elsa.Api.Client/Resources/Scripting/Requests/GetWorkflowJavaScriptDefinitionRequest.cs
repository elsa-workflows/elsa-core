namespace Elsa.Api.Client.Resources.Scripting.Requests;

/// <summary>
/// A request to retrieve the type definitions
/// </summary>
public class GetWorkflowJavaScriptDefinitionRequest(string workflowDefinitionId, string? activityTypeName, string? propertyName)
{
    /// <summary>
    /// Gets or sets workflow definition to retrieve type definitions for.
    /// </summary>
    public string WorkflowDefinitionId { get; set; } = workflowDefinitionId;

    /// <summary>
    /// Gets or sets the type name of the activity to retrieve type definitions for.
    /// </summary>
    public string? ActivityTypeName { get; set; } = activityTypeName;

    /// <summary>
    /// Gets or sets the name of the property to retrieve type definitions for.
    /// </summary>
    public string? PropertyName { get; set; } = propertyName;
}
