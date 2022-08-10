using System;

namespace Elsa.Attributes
{
    public abstract class ActivityPropertyAttributeBase : Attribute
    {
        /// <summary>
        /// The technical name of the activity property.
        /// </summary>
        public string? Name { get; set; }
        
        /// <summary>
        /// A brief description about this property for workflow tooling to use when displaying activity editors.
        /// </summary>
        public string? Hint { get; set; }

        /// <summary>
        /// A value indicating whether this property should be visible.
        /// </summary>
        public bool IsBrowsable { get; set; } = true;

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