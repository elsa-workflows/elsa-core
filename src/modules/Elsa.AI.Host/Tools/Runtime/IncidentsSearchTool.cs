using Elsa.AI.Abstractions.Models;
using Elsa.AI.Host.Services;
using Elsa.Common.Models;

namespace Elsa.AI.Host.Tools.Runtime;

public class IncidentsSearchTool(IServiceProvider serviceProvider, RuntimeGroundingMapper mapper, AIGroundingResultFormatter formatter) : RuntimeToolBase(serviceProvider, formatter)
{
    public override AIToolDefinition Definition { get; } = ReadOnlyDefinition(
        "incidents.search",
        "Search incidents",
        "Search incidents across authorized workflow instances in an explicit workflow, status, or time scope.",
        GroundingToolSchemas.WithProperties(
            ("definitionId", GroundingToolSchemas.String("Workflow definition ID.")),
            ("instanceId", GroundingToolSchemas.String("Workflow instance ID.")),
            ("query", GroundingToolSchemas.String("Free-text incident or instance search term.")),
            ("from", GroundingToolSchemas.String("Updated-at range start.")),
            ("to", GroundingToolSchemas.String("Updated-at range end."))));

    public override async ValueTask<AIToolResult> ExecuteAsync(AIToolExecutionContext context, CancellationToken cancellationToken = default)
    {
        var store = WorkflowInstanceStore;
        if (store == null)
            return InstanceStoreUnavailable();

        var filter = CreateInstanceFilter(context.Arguments);
        filter.HasIncidents = true;
        var page = await store.FindManyAsync(filter, PageArgs.FromRange(0, 100), cancellationToken);
        var incidents = page.Items
            .Where(x => IsTenantAllowed(x, context.TenantId))
            .SelectMany(x => x.WorkflowState.Incidents.Select(incident => mapper.MapIncident(x.Id, incident)))
            .OrderByDescending(x => x.Timestamp)
            .ToList();
        var items = incidents.Select(AIGroundingJson.ToJsonObject);

        return Formatter.CreateResult($"Found {incidents.Count} incidents.", items, incidents.Count, ["WorkflowInstanceStore"]);
    }
}
