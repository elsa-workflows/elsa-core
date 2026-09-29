using System.Diagnostics.CodeAnalysis;
using Elsa.Studio.Models;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Contracts;

/// <summary>
/// Provides functionality for managing app bar items in the application.
/// </summary>
public interface IAppBarService
{
    /// <summary>
    /// Invoked when the app bar items change.
    /// </summary>
    event Action AppBarItemsChanged;

    /// <summary>
    /// Gets the collection of elements displayed in the app bar.
    /// </summary>
    IEnumerable<AppBarElement> AppBarElements { get; }
    
    /// <summary>
    /// A collection of components to render in the app bar.
    /// </summary>
    IEnumerable<RenderFragment> AppBarComponents { get; }
    
    /// <summary>
    /// Adds a component to the app bar.
    /// </summary>
    /// <typeparam name="T">The type of the component.</typeparam>
    [Obsolete("Use AddElement instead.")]
    void AddAppBarItem<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>() where T : IComponent;

    /// <summary>
    /// Adds a component of the specified type to the app bar.
    /// </summary>
    /// <typeparam name="T">The type of the component to add.</typeparam>
    void AddComponent<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(float? order = null) where T : IComponent;

    /// <summary>
    /// Adds an element of the specified type to the app bar.
    /// </summary>
    void AddElement<T>(float? order = null) where T : AppBarElement, new();

    /// <summary>
    /// Adds an element to the app bar.
    /// </summary>
    /// <param name="element">The app bar element to add.</param>
    void AddElement(AppBarElement element);
}
