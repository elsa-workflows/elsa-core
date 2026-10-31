using Elsa.Studio.Contracts;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Models;

/// <summary>
/// Provides a generic implementation of <see cref="IActivityTab"/>.
/// </summary>
public class ActivityTab : IActivityTab
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ActivityTab"/> class.
    /// </summary>
    /// <param name="title">The tab title.</param>
    /// <param name="order">The tab order.</param>
    /// <param name="render">The render delegate.</param>
    public ActivityTab(string title, double order, Func<IDictionary<string, object?>, RenderFragment> render)
    {
        Title = title;
        Order = order;
        Render = render;
    }

    /// <inheritdoc />
    public string Title { get; }

    /// <inheritdoc />
    public string? Icon { get; set; }

    /// <inheritdoc />
    public double Order { get; }

    /// <inheritdoc />
    public bool WrapInScrollableWell { get; set; } = true;

    /// <summary>
    /// Gets or sets the predicate used to determine tab visibility.
    /// </summary>
    public Func<IDictionary<string, object?>, bool>? VisibilityPredicate { get; set; }

    /// <summary>
    /// Gets or sets the icon color provider.
    /// </summary>
    public Func<IDictionary<string, object?>, Color?>? IconColorProvider { get; set; }

    /// <inheritdoc />
    public Func<IDictionary<string, object?>, RenderFragment> Render { get; }

    /// <inheritdoc />
    public bool IsVisible(IDictionary<string, object?> attributes) => VisibilityPredicate?.Invoke(attributes) ?? true;

    /// <inheritdoc />
    public Color? GetIconColor(IDictionary<string, object?> attributes) => IconColorProvider?.Invoke(attributes);
}
