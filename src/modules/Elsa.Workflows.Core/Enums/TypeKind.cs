namespace Elsa.Workflows.Core;

/// <summary>
/// Represents the switch mode.
/// </summary>
[Flags]
public enum TypeKind
{
    Unknown = 0,
    Primitive = 1,
    Object = 2,
    Activity = 4,
    Trigger = 8
}