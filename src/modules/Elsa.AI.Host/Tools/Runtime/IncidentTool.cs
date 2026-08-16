using Elsa.AI.Abstractions.Models;
using Elsa.AI.Host.Services;

namespace Elsa.AI.Host.Tools.Runtime;

public class IncidentTool(IServiceProvider serviceProvider, RuntimeGroundingMapper mapper, AIGroundingResultFormatter formatter) : RuntimeToolBase(serviceProvider, formatter)
{
    public override AIToolDefinition Definition { get; } = ReadOnlyDefinition(
        "incidents.get",
        "Get incident",
        "Get a redacted incident by workflow instance and activity or node ID.",
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
        var incidents = instance.WorkflowState.Incidents
            .Where(x => Matches(x.ActivityId, activityId) || Matches(x.ActivityNodeId, activityNodeId))
            .Select(x => mapper.MapIncident(instance.Id, x))
            .ToList();

        return Formatter.CreateResult($"Resolved {incidents.Count} incidents for workflow instance {instance.Id}.", incidents.Select(AIGroundingJson.ToJsonObject), incidents.Count, ["WorkflowInstanceStore"]);
    }

    private static bool Matches(string? value, string? expected) =>
        !string.IsNullOrWhiteSpace(value) && !string.IsNullOrWhiteSpace(expected) && string.Equals(value, expected, StringComparison.OrdinalIgnoreCase);
}
