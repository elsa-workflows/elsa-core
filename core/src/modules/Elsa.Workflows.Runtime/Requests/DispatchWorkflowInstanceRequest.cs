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
    public string InstanceId { get; init; } = null!;
    
    /// <summary>
    /// The ID of the bookmark to resume.
    /// </summary>
    public string? BookmarkId { get; set; }
    public ActivityHandle? ActivityHandle { get; init; }
    public IDictionary<string, object>? Input { get; init; }
    public IDictionary<string, object>? Properties { get; init; }
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Name of the ingress source that initiated this dispatch. Carried through to the execution cycle registry for
    /// drain-time accounting and the FR-018 inconsistency detection. Null when no attribution is available.
    /// </summary>
    public string? IngressSourceName { get; set; }
}