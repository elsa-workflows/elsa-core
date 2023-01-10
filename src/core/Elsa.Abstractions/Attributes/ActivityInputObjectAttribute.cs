using System;

namespace Elsa.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ActivityInputObjectAttribute : Attribute
    {
        /// <summary>
        /// A category to group these property with - will be overidden by any inputs that specify their own category.
        /// </summary>
        public string? Category { get; set; }
    }
}