using Elsa.AI.Abstractions.Models;
using Elsa.AI.Host.Services;

namespace Elsa.AI.Host.Tools.Workflows;

public class WorkflowDefinitionGraphTool(IServiceProvider serviceProvider, WorkflowGroundingMapper mapper, AIGroundingResultFormatter formatter) : WorkflowToolBase(serviceProvider, formatter)
{
    public override AIToolDefinition Definition { get; } = ReadOnlyDefinition(
        "workflows.getDefinitionGraph",
        "Get workflow graph",
        "Get a bounded graph summary for an authorized workflow definition.",
        GroundingToolSchemas.WithProperties(
            ("id", GroundingToolSchemas.String("Workflow definition version ID.")),
            ("versionId", GroundingToolSchemas.String("Workflow definition version ID.")),
            ("definitionId", GroundingToolSchemas.String("Logical workflow definition ID.")),
            ("version", GroundingToolSchemas.Integer("Specific workflow version."))));

    public override async ValueTask<AIToolResult> ExecuteAsync(AIToolExecutionContext context, CancellationToken cancellationToken = default)
    {
        var store = WorkflowDefinitionStore;
        if (store == null)
            return WorkflowStoreUnavailable();

        var definition = await store.FindAsync(CreateDefinitionFilter(context.Arguments), cancellationToken);
        if (definition == null || !IsTenantAllowed(definition, context.TenantId))
            return new AIToolResult { Status = AIToolInvocationStatus.Failed, Error = "Workflow definition was not found." };

        return Formatter.CreateResult($"Resolved graph for workflow definition {definition.DefinitionId} v{definition.Version}.", [mapper.MapGraph(definition)], 1, ["WorkflowDefinitionStore"]);
    }
}
