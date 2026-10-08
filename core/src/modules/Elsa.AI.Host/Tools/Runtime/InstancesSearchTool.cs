using Elsa.AI.Abstractions.Models;
using Elsa.AI.Host.Services;
using Elsa.Common.Models;

namespace Elsa.AI.Host.Tools.Runtime;

public class InstancesSearchTool(IServiceProvider serviceProvider, RuntimeGroundingMapper mapper, AIGroundingResultFormatter formatter) : RuntimeToolBase(serviceProvider, formatter)
{
    public override AIToolDefinition Definition { get; } = ReadOnlyDefinition(
        "instances.search",
        "Search workflow instances",
        "Search authorized workflow instances by workflow, status, incident flag, correlation ID, and time range.",
        GroundingToolSchemas.WithProperties(
            ("query", GroundingToolSchemas.String("Free-text search term.")),
            ("definitionId", GroundingToolSchemas.String("Workflow definition ID.")),
            ("status", GroundingToolSchemas.String("Workflow status.")),
            ("subStatus", GroundingToolSchemas.String("Workflow sub-status.")),
            ("correlationId", GroundingToolSchemas.String("Correlation ID.")),
            ("hasIncidents", GroundingToolSchemas.Boolean("Whether incidents are present.")),
            ("from", GroundingToolSchemas.String("Updated-at range start.")),
            ("to", GroundingToolSchemas.String("Updated-at range end."))));

    public override async ValueTask<AIToolResult> ExecuteAsync(AIToolExecutionContext context, CancellationToken cancellationToken = default)
    {
        var store = WorkflowInstanceStore;
        if (store == null)
            return InstanceStoreUnavailable();

        var page = await store.FindManyAsync(CreateInstanceFilter(context.Arguments), PageArgs.FromRange(0, 100), cancellationToken);
        var instances = page.Items.Where(x => IsTenantAllowed(x, context.TenantId)).ToList();
        var items = instances.Select(x => AIGroundingJson.ToJsonObject(mapper.Map(x)));

        return Formatter.CreateResult($"Found {instances.Count} authorized workflow instances.", items, instances.Count, ["WorkflowInstanceStore"]);
    }
}
