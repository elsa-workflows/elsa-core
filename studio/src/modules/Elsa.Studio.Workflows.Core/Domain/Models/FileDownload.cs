namespace Elsa.Studio.Workflows.Domain.Models;

/// <summary>
/// Represents a file download.
/// </summary>
public record FileDownload(string FileName, Stream Content);