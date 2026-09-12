using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Abstractions.Models;
using Elsa.AI.Host.Services;

namespace Elsa.AI.Host.Tools.Workflows;

public class WorkflowProposeCreateTool(IServiceProvider serviceProvider, WorkflowDraftValidationService validationService, WorkflowProposalDiffService diffService, AIGroundingResultFormatter formatter) : GroundingToolBase
{
    public override AIToolDefinition Definition { get; } = ProposalDefinition(
        "workflows.proposeCreate",
        "Propose workflow creation",
        "Create a reviewable AI workflow creation proposal. This never persists a workflow definition.",
        GroundingToolSchemas.WithProperties(
            ("draft", GroundingToolSchemas.Object("Workflow draft JSON payload.")),
            ("rationale", GroundingToolSchemas.String("Why the draft should be created."))));

    public override async ValueTask<AIToolResult> ExecuteAsync(AIToolExecutionContext context, CancellationToken cancellationToken = default)
    {
        var store = serviceProvider.GetService(typeof(IAIProposalStore)) as IAIProposalStore;
        if (store == null)
            return formatter.Unavailable("AI proposal store");

        var draft = GetObject(context.Arguments, "draft") ?? [];
        var diagnostics = validationService.Validate(draft);
        var proposal = new AIProposal
        {
            TenantId = context.TenantId,
            ConversationId = context.ConversationId,
            Kind = AIProposalKind.WorkflowCreate,
            Status = diagnostics.Any(x => x.Severity == AIValidationSeverity.Error) ? AIProposalStatus.Blocked : AIProposalStatus.Validated,
            WorkflowPayload = (JsonObject)draft.DeepClone(),
            Rationale = GetString(context.Arguments, "rationale") ?? "",
            ValidationDiagnostics = diagnostics.ToList(),
            GraphDiff = diffService.CreateDiff(draft),
            CreatedBy = context.ActorId
        };
        await store.SaveAsync(proposal, cancellationToken);

        return formatter.CreateResult($"Created workflow creation proposal {proposal.Id}.", [AIGroundingJson.ToJsonObject(proposal)], 1, ["AIProposalStore"]);
    }
}
