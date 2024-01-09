using Elsa.Common.Models;

namespace Elsa.Workflows.Runtime.Requests;

/// <summary>
/// A dispatch request that indicates that one or more workflows are requested to be cancelled.
/// </summary>
/// <remarks>
/// The various sets of properties on this request are mutually exclusive. Only one set of properties should be set.
/// For example, if you want to cancel a specific workflow instance, only set the <see cref="WorkflowInstanceIds"/> property.
/// If you want to cancel all instances of a specific workflow definition, only set the <see cref="DefinitionId"/> property and the <see cref="VersionOptions"/> property.
/// And if you want to cancel all instances of a specific workflow definition version, only set the <see cref="DefinitionVersionId"/> property.
/// </remarks>
public class DispatchCancelWorkflowsRequest
{
    /// <summary>
    /// The IDs of the workflow instances to cancel.
    /// </summary>
    public ICollection<string>? WorkflowInstanceIds { get; set; }
    
    /// <summary>
    /// The ID of the workflow definition to cancel.
    /// </summary>
    public string? DefinitionVersionId { get; set; }
    
    /// <summary>
    /// The definition ID of the workflow definition to cancel. Use this in combination with the <see cref="VersionOptions"/> property.
    /// </summary>
    public string? DefinitionId { get; set; }
    
    /// <summary>
    /// The version options to use when querying for workflow instances to cancel. Use this in combination with the <see cref="DefinitionId"/> property.
    /// </summary>
    public VersionOptions? VersionOptions { get; set; }
}