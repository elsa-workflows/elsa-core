namespace Elsa.Diagnostics.ConsoleLogs.Services;

/// <summary>
/// Well-known metadata keys for console log enrichment.
/// </summary>
public static class ConsoleLogMetadataKeys
{
    /// <summary>
    /// Identifies the workflow instance that owned a console write.
    /// </summary>
    public const string WorkflowInstanceId = "elsa.workflowInstanceId";

    /// <summary>
    /// Identifies the workflow definition that owned a console write.
    /// </summary>
    public const string WorkflowDefinitionId = "elsa.workflowDefinitionId";

    /// <summary>
    /// Identifies the workflow definition version that owned a console write.
    /// </summary>
    public const string WorkflowDefinitionVersionId = "elsa.workflowDefinitionVersionId";

    /// <summary>
    /// Identifies the activity execution instance that owned a console write.
    /// </summary>
    public const string ActivityInstanceId = "elsa.activityInstanceId";

    /// <summary>
    /// Identifies the logical activity that owned a console write.
    /// </summary>
    public const string ActivityId = "elsa.activityId";

    /// <summary>
    /// Identifies the activity node that owned a console write.
    /// </summary>
    public const string ActivityNodeId = "elsa.activityNodeId";
}
