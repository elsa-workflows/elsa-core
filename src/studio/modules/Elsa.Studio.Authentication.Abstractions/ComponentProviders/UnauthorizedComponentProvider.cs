using System.Diagnostics.CodeAnalysis;
using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Authentication.Abstractions.ComponentProviders;

/// <summary>
/// A generic component provider that renders a specific component type.
/// Eliminates the need for separate provider classes for each component.
/// </summary>
/// <typeparam name="TComponent">The component type to render.</typeparam>
public class UnauthorizedComponentProvider<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TComponent> : IUnauthorizedComponentProvider where TComponent : IComponent
{
    /// <inheritdoc />
    public RenderFragment GetUnauthorizedComponent() => builder => builder.CreateComponent<TComponent>();
}
