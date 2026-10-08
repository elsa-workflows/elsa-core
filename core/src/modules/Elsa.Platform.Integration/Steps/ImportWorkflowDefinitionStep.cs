using System.Text.Json;
using Elsa.Platform.Integration.Models;
using Elsa.Platform.Integration.Services;
using Elsa.Workflows;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Models;
using Loom;

namespace Elsa.Platform.Integration.Steps;

[Step("elsa.import-workflow-definition")]
public sealed class ImportWorkflowDefinitionStep(
    IWorkflowDefinitionImporter importer,
    IApiSerializer apiSerializer,
    PlatformRecipeArtifact artifact) : IStep, IValidatingStep
{
    public JsonElement WorkflowDefinition { get; init; }

    public string? Path { get; init; }

    public bool Publish { get; init; } = true;

    public ValueTask<IReadOnlyList<RecipeDiagnostic>> ValidateAsync(
        StepValidationContext context,
        CancellationToken cancellationToken = default)
    {
        var hasInlineWorkflow = WorkflowDefinition.ValueKind == JsonValueKind.Object;
        var hasPath = !string.IsNullOrWhiteSpace(Path);
        if (!hasInlineWorkflow && !hasPath)
        {
            return ValueTask.FromResult<IReadOnlyList<RecipeDiagnostic>>(
            [
                context.Error(
                    "ELSA_PLATFORM_WORKFLOW_DEFINITION_MISSING",
                    "A workflow definition or artifact-relative path is required.",
                    context.Target("input.workflowDefinition"))
            ]);
        }

        if (hasPath && !artifact.TryGetText(Path!, out _))
        {
            return ValueTask.FromResult<IReadOnlyList<RecipeDiagnostic>>(
            [
                context.Error(
                    "ELSA_PLATFORM_WORKFLOW_DEFINITION_FILE_MISSING",
                    $"Workflow definition file '{Path}' was not found in the recipe artifact.",
                    context.Target("input.path"))
            ]);
        }

        return ValueTask.FromResult<IReadOnlyList<RecipeDiagnostic>>([]);
    }

    public async ValueTask ExecuteAsync(StepContext context, CancellationToken cancellationToken = default)
    {
        var workflowJson = ResolveWorkflowJson();
        WorkflowDefinitionModel model;
        try
        {
            model = apiSerializer.Deserialize<WorkflowDefinitionModel>(workflowJson);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException or InvalidOperationException)
        {
            throw new InvalidOperationException("Workflow definition payload is invalid.", ex);
        }

        var importResult = await importer.ImportAsync(new SaveWorkflowDefinitionRequest
        {
            Model = model,
            Publish = Publish
        }, cancellationToken);

        if (!importResult.Succeeded)
        {
            var errors = string.Join("; ", importResult.ValidationErrors.Select(x => x.Message));
            throw new InvalidOperationException($"Workflow definition validation failed: {errors}");
        }

        context.Log($"Workflow definition '{model.DefinitionId}' was imported.");
    }

    private string ResolveWorkflowJson()
    {
        if (!string.IsNullOrWhiteSpace(Path))
        {
            if (artifact.TryGetText(Path, out var content))
                return content;

            throw new InvalidOperationException($"Workflow definition file '{Path}' was not found in the recipe artifact.");
        }

        if (WorkflowDefinition.ValueKind == JsonValueKind.Object)
            return WorkflowDefinition.GetRawText();

        throw new InvalidOperationException("A workflow definition or artifact-relative path is required.");
    }
}
