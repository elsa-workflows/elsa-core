using Elsa.AI.Abstractions.Models;
using Elsa.AI.Host.Services;

namespace Elsa.AI.Host.Tools.Workflows;

public class WorkflowDefinitionTool(IServiceProvider serviceProvider, WorkflowGroundingMapper mapper, AIGroundingResultFormatter formatter) : WorkflowToolBase(serviceProvider, formatter)
{
    public override AIToolDefinition Definition { get; } = ReadOnlyDefinition(
        "workflows.getDefinition",
        "Get workflow definition",
        "Get a model-safe workflow definition summary with version metadata and graph hints.",
        GroundingToolSchemas.WithProperties(
            ("id", GroundingToolSchemas.String("Workflow definition version ID.")),
            ("versionId", GroundingToolSchemas.String("Workflow definition version ID.")),
            ("definitionId", GroundingToolSchemas.String("Logical workflow definition ID.")),
            ("version", GroundingToolSchemas.Integer("Specific workflow version.")),
            ("published", GroundingToolSchemas.Boolean("Resolve the published version instead of the latest version."))));

    public override async ValueTask<AIToolResult> ExecuteAsync(AIToolExecutionContext context, CancellationToken cancellationToken = default)
    {
        var store = WorkflowDefinitionStore;
        if (store == null)
            return WorkflowStoreUnavailable();

        var definition = await store.FindAsync(CreateDefinitionFilter(context.Arguments), cancellationToken);
        if (definition == null || !IsTenantAllowed(definition, context.TenantId))
            return new AIToolResult { Status = AIToolInvocationStatus.Failed, Error = "Workflow definition was not found." };

        return Formatter.CreateResult($"Resolved workflow definition {definition.DefinitionId} v{definition.Version}.", [AIGroundingJson.ToJsonObject(mapper.Map(definition))], 1, ["WorkflowDefinitionStore"]);
    }
}
