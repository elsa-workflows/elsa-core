using System;

namespace Elsa.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ActivityPropertyAttribute : Attribute
    {
        /// <summary>
        /// The technical name of the activity property.
        /// </summary>
        public string? Name { get; set; }
        
        /// <summary>
        /// The user-friendly name of the activity property.
        /// </summary>
        public string? Label { get; set; }
        
        /// <summary>
        /// A hint to workflow tooling what input control to use. 
        /// </summary>
        public string? UIHint { get; set; }
        
        /// <summary>
        /// A brief description about this property for workflow tooling to use when displaying activity editors.
        /// </summary>
        public string? Hint { get; set; }

        /// <summary>
        /// A value representing options specific to a given UI hint.
        /// </summary>
        public object? Options { get; set; }
        
        /// <summary>
        /// The name of a static method that provides options.
        /// </summary>
        public string? OptionsProvider { get; set; }
    }
}