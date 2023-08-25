namespace Elsa.Api.Client.Resources.Scripting.Requests;

/// <summary>
/// A request to retrieve the type definitions
/// </summary>
public class GetWorkflowJavaScriptDefinitionRequest
{
    /// <summary>
    /// Initialize new type definition request.
    /// </summary>
    /// <param name="workflowDefinitionId">Id of workflow definition to retrieve type definitions for.</param>
    /// <param name="activityTypeName">Type name of the activity to retrieve type definitions for.</param>
    /// <param name="propertyName">Name of the property to retrieve type definitions for.</param>
    public GetWorkflowJavaScriptDefinitionRequest(string workflowDefinitionId, string activityTypeName, string propertyName)
    {
        WorkflowDefinitionId = workflowDefinitionId;
        ActivityTypeName = activityTypeName;
        PropertyName = propertyName;
    }

    /// <summary>
    /// Gets or sets workflow definition to retrieve type definitions for.
    /// </summary>
    public string WorkflowDefinitionId { get; set; }
    /// <summary>
    /// Gets or sets type name of the activity to retrieve type definitions for.
    /// </summary>
    public string ActivityTypeName { get; set; }
    /// <summary>
    /// Gets or sets name of the property to retrieve type definitions for.
    /// </summary>
    public string PropertyName { get; set; }
}
