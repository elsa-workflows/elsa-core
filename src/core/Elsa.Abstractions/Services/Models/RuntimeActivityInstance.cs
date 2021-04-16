namespace Elsa.Services.Models
{
    public class RuntimeActivityInstance
    {
        /// <summary>
        /// The activity type.
        /// </summary>
        public ActivityType ActivityType { get; set; } = default!;

        /// <summary>
        /// Unique identifier of this activity within the workflow.
        /// </summary>
        public string Id { get; set; } = default!;

        /// <summary>
        /// Name identifier of this activity.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// A value indicating whether the workflow instance will be persisted automatically upon executing this activity.
        /// </summary>
        public bool PersistWorkflow { get; set; }

        /// <summary>
        /// A value indicating whether the workflow context (if any) will be refreshed automatically before executing this activity. 
        /// </summary>
        public bool LoadWorkflowContext { get; set; }

        /// <summary>
        /// A value indicating whether the workflow context (if any) will be persisted automatically after executing this activity. 
        /// </summary>
        public bool SaveWorkflowContext { get; set; }
        
        public override string ToString() => $"{ActivityType.TypeName} - {Id}";
    }
}