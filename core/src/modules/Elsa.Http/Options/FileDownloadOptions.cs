using System.Net.Http.Headers;

namespace Elsa.Http.Options;

/// <summary>
/// Options for downloading a file.
/// </summary>
public class FileDownloadOptions
{
    /// <summary>
    /// Gets or sets the entity tag.
    /// </summary>
    public EntityTagHeaderValue? ETag { get; set; }

    /// <summary>
    /// Gets or sets the range.
    /// </summary>
    public RangeHeaderValue? Range { get; set; }
}