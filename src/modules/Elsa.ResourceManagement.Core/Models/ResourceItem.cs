using System.Text.Json.Serialization;
using Elsa.ResourceManagement.Serialization.Converters;

namespace Elsa.ResourceManagement;

[JsonConverter(typeof(ResourceConverter))]
public class ResourceItem : ResourceElement
{
    public ResourceItem()
    {
        ResourceItem = this;
    }

    /// <summary>
    /// The logical identifier of the resource across versions.
    /// </summary>
    public string ResourceId { get; set; } = default!;

    /// <summary>
    /// The logical identifier of the versioned resource.
    /// </summary>
    public string ResourceVersionId { get; set; } = default!;

    /// <summary>
    /// The resource type of the resource.
    /// </summary>
    public string ResourceType { get; set; } = default!;

    /// <summary>
    /// Whether the version is published or not.
    /// </summary>
    public bool Published { get; set; }

    /// <summary>
    /// Whether the version is the latest version of the resource item.
    /// </summary>
    public bool Latest { get; set; }

    /// <summary>
    /// When the resource item version has been updated.
    /// </summary>
    public DateTime? ModifiedUtc { get; set; }

    /// <summary>
    /// When the resource item has been published.
    /// </summary>
    public DateTime? PublishedUtc { get; set; }

    /// <summary>
    /// When the resource item has been created or first published.
    /// </summary>
    public DateTime? CreatedUtc { get; set; }

    /// <summary>
    /// The name of the user who first created this resource version.
    /// </summary>
    public string? Owner { get; set; }

    /// <summary>
    /// The name of the user who last modified this resource version.
    /// </summary>
    public string? Author { get; set; }

    /// <summary>
    /// The text representing this resource.
    /// </summary>
    public string DisplayText { get; set; } = string.Empty;

    public override string ToString()
    {
        return string.IsNullOrWhiteSpace(DisplayText) ? $"{ResourceType} ({ResourceId})" : DisplayText;
    }
}