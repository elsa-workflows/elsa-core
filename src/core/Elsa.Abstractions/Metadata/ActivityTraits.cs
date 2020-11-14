using System;

namespace Elsa.Metadata
{
    [Flags]
    public enum ActivityTraits
    {
        Action = 1,
        Trigger = 2,
        Job = 4
    }
}