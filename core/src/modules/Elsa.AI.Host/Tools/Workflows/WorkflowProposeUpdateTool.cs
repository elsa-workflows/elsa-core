using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Abstractions.Models;
using Elsa.AI.Host.Services;

namespace Elsa.AI.Host.Tools.Workflows;

public class WorkflowProposeUpdateTool(IServiceProvider serviceProvider, WorkflowDraftValidationService validationService, WorkflowProposalDiffService diffService, WorkflowGroundingMapper workflowMapper, AIGroundingResultFormatter formatter) : WorkflowToolBase(serviceProvider, formatter)
{
    public override AIToolDefinition Definition { get; } = ProposalDefinition(
        "workflows.proposeUpdate",
        "Propose workflow update",
        "Create a reviewable AI workflow update proposal against a baseline workflow version. This never persists a workflow definition.",
        GroundingToolSchemas.WithProperties(
            ("definitionId", GroundingToolSchemas.String("Logical workflow definition ID.")),
            ("baselineVersionId", GroundingToolSchemas.String("Baseline workflow version ID.")),
            ("draft", GroundingToolSchemas.Object("Workflow draft JSON payload.")),
            ("rationale", GroundingToolSchemas.String("Why the draft should update the workflow."))));

    public override async ValueTask<AIToolResult> ExecuteAsync(AIToolExecutionContext context, CancellationToken cancellationToken = default)
    {
        var proposalStore = ServiceProvider.GetService(typeof(IAIProposalStore)) as IAIProposalStore;
        if (proposalStore == null)
            return Formatter.Unavailable("AI proposal store");

        var workflowStore = WorkflowDefinitionStore;
        if (workflowStore == null)
            return WorkflowStoreUnavailable();

        var baselineVersionId = GetString(context.Arguments, "baselineVersionId");
        var baseline = string.IsNullOrWhiteSpace(baselineVersionId)
            ? null
            : await workflowStore.FindAsync(new() { Id = baselineVersionId }, cancellationToken);
        if (baseline == null || !IsTenantAllowed(baseline, context.TenantId))
            return new AIToolResult { Status = AIToolInvocationStatus.Failed, Error = "Baseline workflow definition was not found." };

        var latest = await workflowStore.FindAsync(new() { DefinitionId = baseline.DefinitionId, VersionOptions = Elsa.Common.Models.VersionOptions.Latest }, cancellationToken);
        var draft = GetObject(context.Arguments, "draft") ?? [];
        var diagnostics = validationService.Validate(draft, baselineVersionId, latest?.Id);
        var proposal = new AIProposal
        {
            TenantId = context.TenantId,
            ConversationId = context.ConversationId,
            Kind = AIProposalKind.WorkflowUpdate,
            Status = diagnostics.Any(x => x.Severity == AIValidationSeverity.Error) ? AIProposalStatus.Blocked : AIProposalStatus.Validated,
            BaselineWorkflowDefinitionId = baseline.DefinitionId,
            BaselineVersionId = baseline.Id,
            WorkflowPayload = (JsonObject)draft.DeepClone(),
            Rationale = GetString(context.Arguments, "rationale") ?? "",
            ValidationDiagnostics = diagnostics.ToList(),
            GraphDiff = diffService.CreateDiff(draft, workflowMapper.MapGraph(baseline)),
            CreatedBy = context.ActorId
        };
        await proposalStore.SaveAsync(proposal, cancellationToken);

        return Formatter.CreateResult($"Created workflow update proposal {proposal.Id}.", [AIGroundingJson.ToJsonObject(proposal)], 1, ["AIProposalStore", "WorkflowDefinitionStore"]);
    }
}
