using Elsa.Studio.DomInterop.Models;

namespace Elsa.Studio.DomInterop.Contracts;

/// <summary>
/// Describes utility methods that interact with DOM elements through JavaScript interop.
/// </summary>
public interface IDomAccessor
{
    /// <summary>
    /// Gets the bounding client rectangle of the specified element.
    /// </summary>
    /// <param name="elementRef">The element reference to get the rectangle for.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The bounding client rectangle of the element.</returns>
    Task<DomRect> GetBoundingClientRectAsync(ElementRef elementRef, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the visible height of the specified element.
    /// </summary>
    /// <param name="elementRef">The element reference to get the visible height for.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The visible height of the element in pixels.</returns>
    Task<double> GetVisibleHeightAsync(ElementRef elementRef, CancellationToken cancellationToken = default);

    /// <summary>
    /// Programmatically clicks the specified element.
    /// </summary>
    /// <param name="elementRef">The element reference to click.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task ClickElementAsync(ElementRef elementRef, CancellationToken cancellationToken = default);
}