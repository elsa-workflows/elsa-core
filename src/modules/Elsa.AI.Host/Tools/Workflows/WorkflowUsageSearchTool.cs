using Elsa.AI.Abstractions.Models;
using Elsa.AI.Host.Services;
using Elsa.Common.Models;
using Elsa.Workflows.Management.Filters;

namespace Elsa.AI.Host.Tools.Workflows;

public class WorkflowUsageSearchTool(IServiceProvider serviceProvider, WorkflowGroundingMapper mapper, AIGroundingResultFormatter formatter) : WorkflowToolBase(serviceProvider, formatter)
{
    public override AIToolDefinition Definition { get; } = ReadOnlyDefinition(
        "workflows.findUsages",
        "Find workflow activity usages",
        "Find authorized workflow definitions that reference an activity type in their serialized graph.",
        GroundingToolSchemas.WithProperties(
            ("activityType", GroundingToolSchemas.String("Activity type to find.")),
            ("query", GroundingToolSchemas.String("Optional workflow search term."))));

    public override async ValueTask<AIToolResult> ExecuteAsync(AIToolExecutionContext context, CancellationToken cancellationToken = default)
    {
        var store = WorkflowDefinitionStore;
        if (store == null)
            return WorkflowStoreUnavailable();

        var activityType = GetString(context.Arguments, "activityType");
        if (string.IsNullOrWhiteSpace(activityType))
            return new AIToolResult { Status = AIToolInvocationStatus.Failed, Error = "activityType is required." };

        var page = await store.FindManyAsync(new WorkflowDefinitionFilter
        {
            SearchTerm = GetString(context.Arguments, "query"),
            VersionOptions = VersionOptions.Latest
        }, PageArgs.FromRange(0, 200), cancellationToken);
        var definitions = page.Items
            .Where(x => IsTenantAllowed(x, context.TenantId))
            .Where(x => mapper.GetGraph(x).ActivityTypes.Contains(activityType, StringComparer.OrdinalIgnoreCase))
            .ToList();
        var items = definitions.Select(x => AIGroundingJson.ToJsonObject(mapper.Map(x)));

        return Formatter.CreateResult($"Found {definitions.Count} workflow definitions using {activityType}.", items, definitions.Count, ["WorkflowDefinitionStore"]);
    }
}
