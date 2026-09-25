using System.Text.Json.Nodes;

namespace Elsa.OrchardCore.Client.Models;

/// <summary>
/// Represents a request to create a new content item in a content management system.
/// </summary>
public class CreateContentItemRequest
{
    /// <summary>
    /// Gets or sets the content type for the new content item.
    /// This determines the structure and behavior of the content item in the CMS.
    /// </summary>
    public string ContentType { get; set; } = null!;

    /// <summary>
    /// Gets or sets the collection of properties for the new content item.
    /// These properties represent custom fields or data associated with the content item.
    /// </summary>
    public JsonNode Properties { get; set; } = null!;

    /// <summary>
    /// Gets or sets a value indicating whether the content item should be published immediately
    /// after it is created. If set to <c>true</c>, the item will be published; otherwise, it will
    /// be saved as a draft.
    /// </summary>
    public bool Publish { get; set; }
}