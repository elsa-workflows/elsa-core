using System;
using Elsa.Metadata;

namespace Elsa.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class TriggerAttribute : ActivityAttribute
    {
        public TriggerAttribute() => Traits = ActivityTraits.Trigger;
    }
}