using Elsa.Workflows;

namespace Elsa.Testing.Framework.Models;

public record ActivityExecutionTrace(string ActivityId, string ActivityNodeId, string ActivityType, string? ActivityName, string Status, DateTimeOffset Timestamp)
{
    public static ActivityExecutionTrace Create(ActivityExecutionContext context)
    {
        return new(
            context.Activity.Id,
            context.Activity.NodeId,
            context.Activity.Type,
            context.Activity.Name,
            context.Status.ToString(),
            DateTimeOffset.UtcNow);
    }
}