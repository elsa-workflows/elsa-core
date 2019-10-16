namespace Elsa.WorkflowDesigner.Models
{
    public class DesignerActivityDefinition
    {
        public DesignerActivityDefinition()
        {
            Type = "Activity";
            Properties = new ActivityPropertyDescriptor[0];
            Category = "Miscellaneous";
            DisplayName = "Activity";
            Properties = new ActivityPropertyDescriptor[0];
            Designer = new ActivityDesignerSettings();
        }

        public string Type { get; set; }
        public string DisplayName { get; set; }
        public string? Description { get; set; }
        public string Category { get; set; }
        public string? Icon { get; set; }
        public ActivityPropertyDescriptor[] Properties { get; set; }
        public ActivityDesignerSettings Designer { get; set; }
    }
}