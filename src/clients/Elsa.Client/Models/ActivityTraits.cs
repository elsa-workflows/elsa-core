using System;

namespace Elsa.Client.Models
{
    [Flags]
    public enum ActivityTraits
    {
        Action = 1,
        Trigger = 2,
        Job = 4
    }
}