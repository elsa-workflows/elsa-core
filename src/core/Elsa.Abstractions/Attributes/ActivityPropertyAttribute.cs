using System;

namespace Elsa.Attributes
{
    [Obsolete("Use ActivityInputAttribute instead.")]
    [AttributeUsage(AttributeTargets.Property)]
    public class ActivityPropertyAttribute : ActivityInputAttribute
    {
    }
}