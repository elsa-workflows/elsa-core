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
    /// Gets the file extensions that this handler supports (without the leading dot, e.g., "json", "elsa").
    /// This is used to optimize blob storage browsing by filtering files before reading their contents.
    /// Return an empty collection if the handler can process any file type.
    /// </summary>
    IEnumerable<string> SupportedExtensions { get; }

    /// <summary>
    /// Returns true if this handler can interpret the given blob based on metadata.
    /// Note: The provider will already have filtered by SupportedExtensions, so this is only
    /// needed for additional validation based on content type or other blob properties.
    /// </summary>
    /// <param name="blob">The blob to check.</param>
    /// <param name="contentType">The content type from blob metadata, if available.</param>
    bool CanHandle(Blob blob, string? contentType);

    /// <summary>
    /// Attempts to parse the blob content into a MaterializedWorkflow.
    /// Returns null if the content is not valid for this handler.
    /// </summary>
    /// <param name="blob">The blob being parsed.</param>
    /// <param name="content">The text content of the blob.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    ValueTask<MaterializedWorkflow?> TryParseAsync(
        Blob blob,
        string content,
        CancellationToken cancellationToken = default);
}
