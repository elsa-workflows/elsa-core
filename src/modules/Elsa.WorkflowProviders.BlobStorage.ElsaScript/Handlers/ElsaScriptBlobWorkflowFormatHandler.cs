using Elsa.Dsl.ElsaScript.Contracts;
using Elsa.Dsl.ElsaScript.Materializers;
using Elsa.WorkflowProviders.BlobStorage.Contracts;
using Elsa.Workflows.Management.Materializers;
using Elsa.Workflows.Runtime;
using FluentStorage.Blobs;
using Microsoft.Extensions.Logging;

namespace Elsa.WorkflowProviders.BlobStorage.ElsaScript.Handlers;

/// <summary>
/// Handles ElsaScript-formatted workflow definitions from blob storage.
/// </summary>
public class ElsaScriptBlobWorkflowFormatHandler(IElsaScriptCompiler compiler, ILogger<ElsaScriptBlobWorkflowFormatHandler> logger) : IBlobWorkflowFormatHandler
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
    public async ValueTask<MaterializedWorkflow?> TryParseAsync(Blob blob, string content, CancellationToken cancellationToken = default)
    {
        try
        {
            var workflow = await compiler.CompileAsync(content, cancellationToken);

            return new MaterializedWorkflow(
                workflow,
                ProviderName: "FluentStorage",
                MaterializerName: ElsaScriptWorkflowMaterializer.MaterializerName,
                MaterializerContext: null,
                OriginalSource: content // Preserve the original ElsaScript source
            );
        }
        catch (Exception ex)
        {
            // Intentionally catching all exceptions here to gracefully handle any failure during workflow parsing.
            // This includes ParseException, IOException, compiler errors, or any unexpected exceptions.
            // Since this is a format handler for user-provided files, we want to log and skip invalid files
            // rather than crashing the workflow loading process.
            logger.LogWarning(ex, "Failed to parse ElsaScript workflow from blob '{BlobPath}'. The file will be skipped.", blob.FullPath);

            // Return null to indicate this handler can't process the content
            return null;
        }
    }
}