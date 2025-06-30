using System.Collections.Generic;
using System.IO;
using Elsa.IO.Extensions;

namespace Elsa.IO.Models;

/// <summary>
/// Represents normalized binary content with metadata.
/// </summary>
public class BinaryContent
{
    /// <summary>
    /// Gets or sets the name of the content.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets the content type (MIME type) based on file extension.
    /// </summary>
    public string? ContentType => !string.IsNullOrWhiteSpace(Name) 
        ? Path.GetExtension(Name).GetContentTypeFromExtension() 
        : null;

    /// <summary>
    /// Gets or sets optional metadata headers.
    /// </summary>
    public IDictionary<string, object> Headers { get; set; } = new Dictionary<string, object>();
    
    /// <summary>
    /// Gets or sets the content stream.
    /// </summary>
    public Stream Stream { get; init; } = null!;
}
