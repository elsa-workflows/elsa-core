using System.Diagnostics.CodeAnalysis;
using Elsa.Studio.Extensions;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Models;

/// <summary>
/// Represents an element in the application bar.
/// Used to define components that can be rendered within the app bar with a specific order.
/// </summary>
/// <remarks>
/// This class can be used to create custom app bar elements by specifying the order and the component to be rendered.
/// The class can be instantiated directly or inherited to create specialized app bar elements.
/// </remarks>
public class AppBarElement
{
    /// <summary>
    /// Gets or sets the order of the element within the application bar.
    /// </summary>
    /// <remarks>
    /// Controls the placement of the element by specifying its position relative to others.
    /// Lower values are placed earlier, and higher values are placed later in the sequence.
    /// </remarks>
    public virtual float Order { get; set; }

    /// <summary>
    /// Gets or sets the component to be rendered in the application bar.
    /// </summary>
    /// <remarks>
    /// Represents the visual content of the app bar element. This content is defined as a render fragment,
    /// allowing for dynamic and customizable rendering within the user interface.
    /// </remarks>
    public virtual RenderFragment Component { get; set; } = null!;
}

/// <summary>
/// Represents an element in the application bar with a specific type constraint for its component.
/// Used to define components of a specific type that can be rendered within the app bar.
/// </summary>
/// <typeparam name="T">The type of the component to be rendered, which must implement <see cref="IComponent"/>.</typeparam>
/// <remarks>
/// This generic specialization of the `AppBarElement` class enforces that only components of the specified type
/// are allowed. The rendering logic uses the type parameter to instantiate the specified component.
/// </remarks>
public class AppBarElement<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T> : AppBarElement where T : IComponent
{
    /// <inheritdoc />
    public override RenderFragment Component { get; set; } = builder => builder.CreateComponent<T>();
}
