using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Contracts;

/// <summary>
/// Represents a widget.
/// </summary>
public interface IWidget
{
    /// <summary>
    /// Gets the zone in which the widget should be rendered.
    /// </summary>
    string Zone { get; }
    
    /// <summary>
    /// Gets the order in which the widget should be rendered.
    /// </summary>
    double Order { get; }
    
    /// <summary>
    /// Gets the fragment to render.
    /// </summary>
    Func<IDictionary<string, object?>, RenderFragment> Render { get; }
}

