using System;

namespace Elsa.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ActivityInputAttribute : Attribute
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
        /// The type that provides options.
        /// </summary>
        public Type? OptionsProvider { get; set; }

        /// <summary>
        /// A category to group this property with.
        /// </summary>
        public string? Category { get; set; }
        
        /// <summary>
        /// A value to order this property by. Properties are displayed in ascending order (lower appears before higher).
        /// </summary>
        public float Order { get; set; }
        
        /// <summary>
        /// The default value to set.
        /// </summary>
        public object? DefaultValue { get; set; }
        
        /// <summary>
        /// The type that provides a default value.
        /// </summary>
        public Type? DefaultValueProvider { get; set; }
        
        /// <summary>
        /// The syntax to use by default when evaluating the value. Only used when the property definition doesn't have a syntax specified. 
        /// </summary>
        public string? DefaultSyntax { get; set; }
        
        /// <summary>
        /// The syntax to use by default when evaluating the value. Only used when the property definition doesn't have a syntax specified. 
        /// </summary>
        public string[]? SupportedSyntaxes { get; set; }
        
        /// <summary>
        /// A value indicating whether this property should be displayed but as read-only.
        /// </summary>
        public bool? IsReadOnly { get; set; }
        
        /// <summary>
        /// A value indicating whether this property should be visible.
        /// </summary>
        public bool? IsBrowsable { get; set; }
        
        /// <summary>
        /// A value indicating whether this property MUST be evaluated at runtime to support the workflow designer.
        /// </summary>
        public bool IsDesignerCritical { get; set; }
        
        /// <summary>
        /// The workflow storage provider to use by default to store the output value.
        /// </summary>
        public string? DefaultWorkflowStorageProvider { get; set; }

        /// <summary>
        /// A flag indicating whether or not the user is allowed to select a workflow provider.
        /// </summary>
        public bool DisableWorkflowProviderSelection { get; set; }
    }
}