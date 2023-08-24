using Elsa.Telnyx.Models;
using Elsa.Workflows.Core;

namespace Elsa.Telnyx.Extensions;

/// <summary>
/// Provides extensions on <see cref="ActivityExecutionContext"/>.
/// </summary>
public static class ActivityExecutionExtensions
{
    /// <summary>
    /// Creates a correlating client state.
    /// </summary>
    public static string CreateCorrelatingClientState(this ActivityExecutionContext context, string? activityInstanceId = default)
    {
        return new ClientStatePayload(context.WorkflowExecutionContext.Id, activityInstanceId).ToBase64();
    }
}