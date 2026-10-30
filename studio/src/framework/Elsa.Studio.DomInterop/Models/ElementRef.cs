using Elsa.Studio.Models;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.DomInterop.Models;

/// <summary>
/// Represents a reference to a DOM element that can be supplied as either an element identifier or a <see cref="ElementReference"/>.
/// </summary>
public class ElementRef : Union<string, ElementReference>
{
    /// <summary>
    /// Creates an <see cref="ElementRef"/> from a string element identifier.
    /// </summary>
    /// <param name="value">The DOM element identifier.</param>
    private ElementRef(string value) : base(value) { }

    /// <summary>
    /// Creates an <see cref="ElementRef"/> from a Blazor <see cref="ElementReference"/>.
    /// </summary>
    /// <param name="value">The referenced element.</param>
    private ElementRef(ElementReference value) : base(value) { }

    /// <summary>
    /// Implicitly converts a string identifier to an <see cref="ElementRef"/>.
    /// </summary>
    /// <param name="value">The DOM element identifier.</param>
    /// <returns>An <see cref="ElementRef"/> referencing the identifier.</returns>
    public static implicit operator ElementRef(string value) => new(value);

    /// <summary>
    /// Implicitly converts an <see cref="ElementReference"/> to an <see cref="ElementRef"/>.
    /// </summary>
    /// <param name="value">The DOM element reference.</param>
    /// <returns>An <see cref="ElementRef"/> referencing the element.</returns>
    public static implicit operator ElementRef(ElementReference value) => new(value);
}