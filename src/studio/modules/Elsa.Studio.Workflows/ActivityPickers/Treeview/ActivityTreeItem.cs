using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using MudBlazor;

namespace Elsa.Studio.Workflows.Models;

/// <summary>
/// Represents an item in an activity tree.
/// </summary>
/// <remarks>This class is designed to hold hierarchical data for activities, including metadata such as
/// category paths and icon colors. It extends <see cref="TreeItemData{T}"/> with additional properties specific to
/// activity-related data.</remarks>
public class ActivityTreeItem : TreeItemData<string>
{
    /// <summary>
    /// Gets or sets the full category path for the activity
    /// </summary>
    public string CategoryPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the color of the icon as a string representation.
    /// </summary>
    public string IconColor { get; set; } = string.Empty;

    /// <summary>
    /// Gets the SVG icon string with the specified color applied.
    /// </summary>
    /// <remarks>This property modifies the "stroke" and "fill" attributes of the SVG icon string to
    /// use the value of <see cref="IconColor"/>. If <see cref="Icon"/> is null, an empty string is
    /// returned.</remarks>
    public string IconWithColor => string.IsNullOrWhiteSpace(IconColor)
        ? Icon ?? string.Empty
        : Icon?
            .Replace("stroke=\"currentColor\"", $"stroke=\"{IconColor}\"")
            .Replace("fill=\"currentColor\"", $"fill=\"{IconColor}\"") ?? string.Empty;

    /// <summary>
    /// The activity descriptor
    /// </summary>
    public ActivityDescriptor ActivityDescriptor { get; set; } = null!;

    /// <summary>
    /// The mutable list backing the <see cref="TreeItemData{T}.Children"/> property.
    /// </summary>
    public List<ITreeItemData<string>> ChildrenList { get; }

    /// <summary>
    /// Represents an item in an activity tree, containing text and child items.
    /// </summary>
    /// <param name="text">The text associated with this activity tree item. Cannot be null or empty.</param>
    public ActivityTreeItem(string text)
    {
        Text = text;
        ChildrenList = [];
        Children = ChildrenList;
    }
}
