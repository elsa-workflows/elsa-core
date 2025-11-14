using Elsa.Scripting.ElsaScript.Lowering;
using Elsa.WorkflowProviders.BlobStorage.Contracts;
using Elsa.Workflows.Runtime;
using FluentStorage.Blobs;

namespace Elsa.WorkflowProviders.BlobStorage.ElsaScript.Handlers;

/// <summary>
/// Handles ElsaScript-formatted workflow definitions from blob storage.
/// </summary>
public class ElsaScriptBlobWorkflowFormatHandler(ElsaScriptCompiler compiler) : IBlobWorkflowFormatHandler
{
    /// <inheritdoc />
    public string Name => "ElsaScript";

    /// <inheritdoc />
    public bool CanHandle(Blob blob, string? contentType, string? extension)
    {
        if (!string.IsNullOrEmpty(extension) && extension.Equals("elsa", StringComparison.OrdinalIgnoreCase))
            return true;

        if (!string.IsNullOrEmpty(contentType) && contentType.Contains("elsa", StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    /// <inheritdoc />
    public ValueTask<MaterializedWorkflow?> TryParseAsync(Blob blob, string content, CancellationToken cancellationToken = default)
    {
        try
        {
            var workflow = compiler.Compile(content);

            var materialized = new MaterializedWorkflow(
                workflow,
                ProviderName: "FluentStorage",
                MaterializerName: "ElsaScript"
            );

            return new(materialized);
        }
        catch
        {
            // If ElsaScript parsing fails, return null to indicate this handler can't process the content
            return new((MaterializedWorkflow?)null);
        }
    }
}
