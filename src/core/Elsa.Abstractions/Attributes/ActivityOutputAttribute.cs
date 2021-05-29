using System;

namespace Elsa.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ActivityOutputAttribute : Attribute
    {
        /// <summary>
        /// A brief description about this property for workflow tooling to use when displaying activity editors.
        /// </summary>
        public string? Hint { get; set; }
    }
}