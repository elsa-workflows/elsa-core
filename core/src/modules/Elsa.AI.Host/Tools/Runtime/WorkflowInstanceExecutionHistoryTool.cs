using Elsa.AI.Abstractions.Models;
using Elsa.AI.Host.Services;

namespace Elsa.AI.Host.Tools.Runtime;

public class WorkflowInstanceExecutionHistoryTool(IServiceProvider serviceProvider, RuntimeGroundingMapper mapper, AIGroundingResultFormatter formatter) : RuntimeToolBase(serviceProvider, formatter)
{
    public override AIToolDefinition Definition { get; } = ReadOnlyDefinition(
        "instances.getExecutionHistory",
        "Get instance execution history",
        "Get bounded execution-history evidence from the workflow instance state.",
        GroundingToolSchemas.WithProperties(
            ("instanceId", GroundingToolSchemas.String("Workflow instance ID.")),
            ("id", GroundingToolSchemas.String("Workflow instance ID."))));

    public override async ValueTask<AIToolResult> ExecuteAsync(AIToolExecutionContext context, CancellationToken cancellationToken = default)
    {
        if (WorkflowInstanceStore == null)
            return InstanceStoreUnavailable();

        var instance = await FindAuthorizedInstanceAsync(context.Arguments, context.TenantId, cancellationToken);
        if (instance == null)
            return new AIToolResult { Status = AIToolInvocationStatus.Failed, Error = "Workflow instance was not found." };

        var item = new JsonObject
        {
            ["instance"] = AIGroundingJson.ToJsonObject(mapper.Map(instance)),
            ["scheduledActivities"] = AIGroundingJson.ToJsonArray(instance.WorkflowState.ScheduledActivities.Select(x => new { x.ActivityNodeId, x.OwnerContextId, x.ExistingActivityExecutionContextId })),
            ["bookmarks"] = AIGroundingJson.ToJsonArray(instance.WorkflowState.Bookmarks.Select(x => new { x.Id, x.Name, x.ActivityId })),
            ["incidents"] = AIGroundingJson.ToJsonArray(instance.WorkflowState.Incidents.Select(x => mapper.MapIncident(instance.Id, x)).OrderBy(x => x.Timestamp))
        };

        return Formatter.CreateResult($"Resolved execution history for workflow instance {instance.Id}.", [item], 1, ["WorkflowInstanceStore"]);
    }
}
