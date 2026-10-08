using Elsa.Dsl.ElsaScript.Contracts;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;

namespace Elsa.Dsl.ElsaScript.Materializers;

/// <summary>
/// Materializes a <see cref="Workflow"/> from a <see cref="WorkflowDefinition"/>'s ElsaScript source code.
/// </summary>
public class ElsaScriptWorkflowMaterializer(IElsaScriptCompiler compiler) : IWorkflowMaterializer
{
    /// <summary>
    /// The name of the materializer.
    /// </summary>
    public const string MaterializerName = "ElsaScript";

    /// <inheritdoc />
    public string Name => MaterializerName;

    /// <inheritdoc />
    public async ValueTask<Workflow> MaterializeAsync(WorkflowDefinition definition, CancellationToken cancellationToken)
    {
        var source = definition.OriginalSource ?? string.Empty;
        var workflow = await compiler.CompileAsync(source, cancellationToken);

        // Preserve the workflow definition ID and version
        workflow.Identity = new(
            definition.DefinitionId,
            definition.Version,
            definition.Id,
            definition.TenantId
        );

        return workflow;
    }
}
