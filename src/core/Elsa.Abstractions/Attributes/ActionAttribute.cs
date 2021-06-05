using System;
using Elsa.Metadata;

namespace Elsa.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ActionAttribute : ActivityAttribute
    {
        public ActionAttribute() => Traits = ActivityTraits.Action;
    }
}