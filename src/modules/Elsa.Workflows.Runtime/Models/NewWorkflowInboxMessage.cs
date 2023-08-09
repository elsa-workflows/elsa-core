using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Helpers;

namespace Elsa.Workflows.Runtime.Models;

/// <summary>
/// A message that can be delivered to a workflow instance.
/// </summary>
public class NewWorkflowInboxMessage
{
    private static readonly TimeSpan DefaultTimeToLive = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Creates a new instance of the <see cref="NewWorkflowInboxMessage"/> class.
    /// </summary>
    /// <param name="bookmarkPayload">The bookmark payload to use to filter the workflow instances to deliver the message to.</param>
    /// <param name="workflowInstanceId">An optional workflow instance ID to select the workflow instance to deliver the message to.</param>
    /// <param name="correlationId">An optional correlation ID to select the workflow instance to deliver the message to.</param>
    /// <param name="activityInstanceId">An optional activity instance ID to select the workflow instance to deliver the message to.</param>
    /// <param name="input">An optional set of inputs to deliver to the workflow instance.</param>
    /// <param name="timeToLive">The duration after which the message expires.</param>
    /// <typeparam name="T">The type of the activity to deliver the message to.</typeparam>
    /// <returns>A new instance of the <see cref="NewWorkflowInboxMessage"/> class.</returns>
    public static NewWorkflowInboxMessage For<T>(
        object bookmarkPayload, 
        string? workflowInstanceId = default, 
        string? correlationId = default, 
        string? activityInstanceId = default,
        IDictionary<string, object>? input = default, 
        TimeSpan? timeToLive = default) where T:IActivity
    {
        return new NewWorkflowInboxMessage
        {
            Input = input,
            BookmarkPayload = bookmarkPayload,
            CorrelationId = correlationId,
            WorkflowInstanceId = workflowInstanceId,
            ActivityInstanceId = activityInstanceId,
            ActivityTypeName = ActivityTypeNameHelper.GenerateTypeName<T>(),
            TimeToLive = timeToLive ?? DefaultTimeToLive,
        };
    }
    
    /// <summary>
    /// The type name of the activity to deliver the message to.
    /// </summary>
    public string ActivityTypeName { get; set; } = default!;

    /// <summary>
    /// An optional bookmark payload that can be used to filter the workflow instances to deliver the message to.
    /// </summary>
    public object BookmarkPayload { get; set; } = default!;
    
    /// <summary>
    /// An optional workflow instance ID to select the workflow instance to deliver the message to.
    /// </summary>
    public string? WorkflowInstanceId { get; set; }

    /// <summary>
    /// An optional activity instance ID to select the workflow instance to deliver the message to.
    /// </summary>
    public string? ActivityInstanceId { get; set; }

    /// <summary>
    /// An optional correlation ID to select the workflow instance to deliver the message to.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// An optional set of inputs to deliver to the workflow instance.
    /// </summary>
    public IDictionary<string, object>? Input { get; set; }

    /// <summary>
    /// The duration after which the message expires.
    /// </summary>
    public TimeSpan TimeToLive { get; set; } = DefaultTimeToLive;
}