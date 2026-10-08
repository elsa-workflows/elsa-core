using Elsa.AI.Abstractions.Models;
using Elsa.AI.Host.Services;

namespace Elsa.AI.Host.Tools.Runtime;

public class WorkflowInstanceActivityStateTool(IServiceProvider serviceProvider, AIGroundingResultFormatter formatter) : RuntimeToolBase(serviceProvider, formatter)
{
    public override AIToolDefinition Definition { get; } = ReadOnlyDefinition(
        "instances.getActivityState",
        "Get instance activity state",
        "Get bounded state for a specific activity in a workflow instance.",
        GroundingToolSchemas.WithProperties(
            ("instanceId", GroundingToolSchemas.String("Workflow instance ID.")),
            ("activityId", GroundingToolSchemas.String("Activity ID.")),
            ("activityNodeId", GroundingToolSchemas.String("Activity node ID."))));

    public override async ValueTask<AIToolResult> ExecuteAsync(AIToolExecutionContext context, CancellationToken cancellationToken = default)
    {
        if (WorkflowInstanceStore == null)
            return InstanceStoreUnavailable();

        var instance = await FindAuthorizedInstanceAsync(context.Arguments, context.TenantId, cancellationToken);
        if (instance == null)
            return new AIToolResult { Status = AIToolInvocationStatus.Failed, Error = "Workflow instance was not found." };

        var activityId = GetString(context.Arguments, "activityId");
        var activityNodeId = GetString(context.Arguments, "activityNodeId");
        var item = new JsonObject
        {
            ["instanceId"] = instance.Id,
            ["activityId"] = activityId,
            ["activityNodeId"] = activityNodeId,
            ["incidents"] = AIGroundingJson.ToJsonArray(instance.WorkflowState.Incidents
                .Where(x => Matches(x.ActivityId, activityId) || Matches(x.ActivityNodeId, activityNodeId))
                .Select(x => new { x.ActivityId, x.ActivityNodeId, x.ActivityType, x.Message, x.Timestamp })),
            ["scheduledActivities"] = AIGroundingJson.ToJsonArray(instance.WorkflowState.ScheduledActivities
                .Where(x => Matches(x.ActivityNodeId, activityNodeId) || Matches(x.ActivityNodeId, activityId))
                .Select(x => new { x.ActivityNodeId, x.OwnerContextId, x.ExistingActivityExecutionContextId })),
            ["bookmarks"] = AIGroundingJson.ToJsonArray(instance.WorkflowState.Bookmarks
                .Where(x => Matches(x.ActivityId, activityId))
                .Select(x => new { x.Id, x.Name, x.ActivityId }))
        };

        return Formatter.CreateResult($"Resolved activity state for workflow instance {instance.Id}.", [Formatter.RedactObject(item)], 1, ["WorkflowInstanceStore"]);
    }

    private static bool Matches(string? value, string? expected) =>
        !string.IsNullOrWhiteSpace(value) && !string.IsNullOrWhiteSpace(expected) && string.Equals(value, expected, StringComparison.OrdinalIgnoreCase);
}
