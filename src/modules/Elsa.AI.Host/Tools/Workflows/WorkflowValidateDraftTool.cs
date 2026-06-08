using Elsa.AI.Abstractions.Models;
using Elsa.AI.Host.Services;

namespace Elsa.AI.Host.Tools.Workflows;

public class WorkflowValidateDraftTool(WorkflowDraftValidationService validationService, AIGroundingResultFormatter formatter) : GroundingToolBase
{
    public override AIToolDefinition Definition { get; } = ReadOnlyDefinition(
        "workflows.validateDraft",
        "Validate workflow draft",
        "Validate a workflow draft against installed activity descriptors and baseline metadata.",
        GroundingToolSchemas.WithProperties(
            ("draft", GroundingToolSchemas.Object("Workflow draft JSON payload.")),
            ("baselineVersionId", GroundingToolSchemas.String("Baseline workflow version ID.")),
            ("expectedBaselineVersionId", GroundingToolSchemas.String("Expected current baseline version ID."))));

    public override ValueTask<AIToolResult> ExecuteAsync(AIToolExecutionContext context, CancellationToken cancellationToken = default)
    {
        var draft = GetObject(context.Arguments, "draft") ?? [];
        var diagnostics = validationService.Validate(draft, GetString(context.Arguments, "baselineVersionId"), GetString(context.Arguments, "expectedBaselineVersionId"));
        var hasErrors = diagnostics.Any(x => x.Severity == AIValidationSeverity.Error);
        var result = formatter.CreateResult(
            hasErrors ? "Workflow draft validation failed." : "Workflow draft validation completed.",
            diagnostics.Select(AIGroundingJson.ToJsonObject),
            diagnostics.Count,
            ["ActivityRegistry"]);

        return ValueTask.FromResult(result with { Status = hasErrors ? AIToolInvocationStatus.Failed : AIToolInvocationStatus.Completed });
    }
}
