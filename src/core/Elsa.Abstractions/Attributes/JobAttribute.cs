using System;
using Elsa.Metadata;

namespace Elsa.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class JobAttribute : ActivityAttribute
    {
        public JobAttribute() => Traits = ActivityTraits.Job;
    }
}