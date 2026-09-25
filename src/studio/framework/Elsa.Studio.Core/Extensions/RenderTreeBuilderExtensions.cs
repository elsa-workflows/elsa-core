using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Elsa.Studio.Extensions;

/// <summary>
/// Provides extension methods for the <see cref="RenderTreeBuilder"/> class.
/// </summary>
public static class RenderTreeBuilderExtensions
{
    /// <summary>
    /// Creates a component of the specified type.
    /// </summary>
    public static void CreateComponent<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(this RenderTreeBuilder builder) where T : IComponent
    {
        var sequence = 0;
        CreateComponent<T>(builder, ref sequence);
    }

    /// <summary>
    /// Creates a component of the specified type.
    /// </summary>
    public static void CreateComponent<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(this RenderTreeBuilder builder, ref int sequence) where T : IComponent
    {
        builder.OpenComponent<T>(sequence++);
        builder.CloseComponent();
    }

    /// <summary>
    /// Creates a component of the specified type at the specified sequence.
    /// </summary>
    /// <param name="builder">The <see cref="RenderTreeBuilder"/> to append the component to.</param>
    /// <param name="componentType">The type of the component to create.</param>
    public static void CreateComponent(this RenderTreeBuilder builder, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type componentType)
    {
        var sequence = 0;
        CreateComponent(builder, ref sequence, componentType);
    }
    
    /// <summary>
    /// Creates a component of the specified type at the specified sequence.
    /// </summary>
    /// <param name="builder">The <see cref="RenderTreeBuilder"/> to append the component to.</param>
    /// <param name="sequence">The sequence number for the component.</param>
    /// <param name="componentType">The type of the component to create.</param>
    public static void CreateComponent(this RenderTreeBuilder builder, ref int sequence, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type componentType)
    {
        builder.OpenComponent(sequence++, componentType);
        builder.CloseComponent();
    }
}
