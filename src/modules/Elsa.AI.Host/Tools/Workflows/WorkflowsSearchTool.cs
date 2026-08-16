using Elsa.AI.Abstractions.Models;
using Elsa.AI.Host.Services;
using Elsa.Common.Models;

namespace Elsa.AI.Host.Tools.Workflows;

public class WorkflowsSearchTool(IServiceProvider serviceProvider, WorkflowGroundingMapper mapper, AIGroundingResultFormatter formatter) : WorkflowToolBase(serviceProvider, formatter)
{
    public override AIToolDefinition Definition { get; } = ReadOnlyDefinition(
        "workflows.search",
        "Search workflows",
        "Search authorized workflow definitions by name, id, description, and version scope.",
        GroundingToolSchemas.WithProperties(
            ("query", GroundingToolSchemas.String("Free-text search term.")),
            ("name", GroundingToolSchemas.String("Workflow name.")),
            ("definitionId", GroundingToolSchemas.String("Logical workflow definition ID.")),
            ("version", GroundingToolSchemas.Integer("Specific workflow version.")),
            ("published", GroundingToolSchemas.Boolean("Search the published version instead of the latest version."))));

    public override async ValueTask<AIToolResult> ExecuteAsync(AIToolExecutionContext context, CancellationToken cancellationToken = default)
    {
        var store = WorkflowDefinitionStore;
        if (store == null)
            return WorkflowStoreUnavailable();

        var page = await store.FindManyAsync(CreateDefinitionFilter(context.Arguments), PageArgs.FromRange(0, 100), cancellationToken);
        var definitions = page.Items.Where(x => IsTenantAllowed(x, context.TenantId)).ToList();
        var items = definitions.Select(x => AIGroundingJson.ToJsonObject(mapper.Map(x)));

        return Formatter.CreateResult($"Found {definitions.Count} authorized workflow definitions.", items, definitions.Count, ["WorkflowDefinitionStore"]);
    }
}
