using System;

namespace Elsa.Workflows.Core.Models;

[Flags]
public enum TypeKind
{
    Unknown = 0,
    Primitive = 1,
    Object = 2,
    Activity = 4,
    Trigger = 8
}