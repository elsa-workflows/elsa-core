using System.Text.Json.Serialization;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.Runtime.Requests;

/// <summary>
/// A request to dispatch a workflow instance for execution.
/// </summary>
public class DispatchWorkflowInstanceRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DispatchWorkflowInstanceRequest"/> class.
    /// </summary>
    [JsonConstructor]
    public DispatchWorkflowInstanceRequest()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DispatchWorkflowInstanceRequest"/> class.
    /// </summary>
    /// <param name="instanceId">The ID of the workflow instance to dispatch.</param>
    public DispatchWorkflowInstanceRequest(string instanceId)
    {
        InstanceId = instanceId;
    }
    
    /// <summary>
    /// The ID of the workflow instance to dispatch.
    /// </summary>
    public string InstanceId { get; init; } = default!;
    
    /// <summary>
    /// The ID of the bookmark to resume.
    /// </summary>
    public string? BookmarkId { get; set; }
    public ActivityHandle? ActivityHandle { get; init; }
    public IDictionary<string, object>? Input { get; init; }
    public IDictionary<string, object>? Properties { get; init; }
    public string? CorrelationId { get; init; }
}