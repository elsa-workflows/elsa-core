using Elsa.AI.Abstractions.Models;
using Elsa.AI.Host.Services;

namespace Elsa.AI.Host.Tools.Runtime;

public class WorkflowInstanceTool(IServiceProvider serviceProvider, RuntimeGroundingMapper mapper, AIGroundingResultFormatter formatter) : RuntimeToolBase(serviceProvider, formatter)
{
    public override AIToolDefinition Definition { get; } = ReadOnlyDefinition(
        "instances.get",
        "Get workflow instance",
        "Get a redacted workflow instance summary and bounded state evidence.",
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

        var item = AIGroundingJson.ToJsonObject(mapper.Map(instance));
        item["state"] = mapper.MapState(instance);

        return Formatter.CreateResult($"Resolved workflow instance {instance.Id}.", [item], 1, ["WorkflowInstanceStore"]);
    }
}
