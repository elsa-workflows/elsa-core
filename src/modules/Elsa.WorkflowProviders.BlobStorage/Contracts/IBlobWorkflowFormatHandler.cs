using Elsa.Workflows.Runtime;
using FluentStorage.Blobs;

namespace Elsa.WorkflowProviders.BlobStorage.Contracts;

/// <summary>
/// Handles parsing of workflow definitions from blob storage in a specific format.
/// </summary>
public interface IBlobWorkflowFormatHandler
{
    /// <summary>
    /// A short name or ID of the handler, for diagnostics/logging.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Returns true if this handler can interpret the given blob based on metadata.
    /// </summary>
    /// <param name="blob">The blob to check.</param>
    /// <param name="contentType">The content type from blob metadata, if available.</param>
    /// <param name="extension">The file extension without the dot (e.g., "json", "elsa").</param>
    bool CanHandle(Blob blob, string? contentType, string? extension);

    /// <summary>
    /// Attempts to parse the blob content into a MaterializedWorkflow.
    /// Returns null if the content is not a valid workflow for this handler.
    /// </summary>
    /// <param name="blob">The blob being parsed.</param>
    /// <param name="content">The text content of the blob.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask<MaterializedWorkflow?> TryParseAsync(
        Blob blob,
        string content,
        CancellationToken cancellationToken = default);
}
