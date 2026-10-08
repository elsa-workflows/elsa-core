using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Contracts;

/// <summary>
/// Represents a contract for defining activity tabs within the application.
/// </summary>
public interface IActivityTab
{
    /// <summary>
    /// Gets the title of the activity tab. This property represents a user-facing label or name
    /// for the tab within the user interface, displayed as the tab header text.
    /// </summary>
    string Title { get; }

    /// <summary>
    /// Gets the icon to render for the activity tab.
    /// </summary>
    string? Icon => null;

    /// <summary>
    /// Gets the order of the tab in relation to other tabs.
    /// This property determines the relative position of the tab when displayed, allowing tabs to
    /// be sorted or organized based on their numerical order value.
    /// </summary>
    double Order { get; }

    /// <summary>
    /// Gets a value indicating whether the tab content should be wrapped in a <c>ScrollableWell</c> by the caller.
    /// </summary>
    bool WrapInScrollableWell => true;

    /// <summary>
    /// Determines whether the tab should be visible for the provided attributes.
    /// </summary>
    /// <param name="attributes">The activity tab attributes.</param>
    /// <returns><see langword="true"/> if the tab should be rendered; otherwise, <see langword="false"/>.</returns>
    bool IsVisible(IDictionary<string, object?> attributes) => true;

    /// <summary>
    /// Gets the icon color for the tab.
    /// </summary>
    /// <param name="attributes">The activity tab attributes.</param>
    /// <returns>The icon color to use, if any.</returns>
    Color? GetIconColor(IDictionary<string, object?> attributes) => null;

    /// <summary>
    /// Defines a rendering function for an activity tab. This function takes a dictionary of attributes
    /// as its input and returns a <see cref="RenderFragment"/> that describes the content to be rendered
    /// within the tab.
    /// </summary>
    Func<IDictionary<string, object?>, RenderFragment> Render { get; }
}
