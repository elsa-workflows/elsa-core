namespace Elsa.Metadata
{
    public class ActivityDescriptor
    {
        public ActivityDescriptor()
        {
            Type = "Activity";
            Properties = new ActivityPropertyDescriptor[0];
            Category = "Miscellaneous";
            DisplayName = "Activity";
            Properties = new ActivityPropertyDescriptor[0];
            Outcomes = null;
        }

        public string Type { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string RuntimeDescription { get; set; }
        public string Category { get; set; }
        public string Icon { get; set; }
        public object Outcomes { get; set; }
        public ActivityPropertyDescriptor[] Properties { get; set; }
    }
}