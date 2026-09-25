using System.Diagnostics.CodeAnalysis;

namespace Elsa.DropIns.Models;

public class DropInDescriptor([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type type)
{
    /// <summary>
    /// Gets or sets the type of the drop-in. 
    /// The DynamicallyAccessedMembers attribute ensures that the IL2072 warning is suppressed.
    /// </summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
    public Type Type { get; set; } = type;
}