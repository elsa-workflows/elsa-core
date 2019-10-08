namespace Elsa.WorkflowDesigner.Models
{
    public class ActivityDefinition
    {
        public ActivityDefinition()
        {
            Properties = new ActivityPropertyDescriptor[0];
        }

        public string Type { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public string Icon { get; set; }
        public ActivityPropertyDescriptor[] Properties { get; set; }
        public ActivityDesignerSettings Designer { get; set; }
    }
}