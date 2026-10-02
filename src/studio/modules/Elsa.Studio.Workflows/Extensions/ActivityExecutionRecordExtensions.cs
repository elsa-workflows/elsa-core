using Elsa.Api.Client.Resources.ActivityExecutions.Models;

namespace Elsa.Studio.Workflows.Extensions;

/// <summary>
/// Provides extension methods for <see cref="ActivityExecutionRecord"/>.
/// </summary>
public static class ActivityExecutionRecordExtensions
{
    /// <summary>
    /// Determines if the specified activity execution record is fused.
    /// </summary>
    public static bool IsFused(this ActivityExecutionRecord record)
    {
        return record.ActivityState != null || record.Outputs != null || record.Exception != null || record.Payload != null || record.Properties is { Count: > 0 };
    }
}