namespace Elsa.Metadata
{
    public class ActivityDescriptor
    {
        public ActivityDescriptor()
        {
            Type = "Activity";
            Category = "Miscellaneous";
            Traits = ActivityTraits.Action;
            DisplayName = "Activity";
            Properties = new ActivityPropertyDescriptor[0];
            Outcomes = new string[0];
        }

        public string Type { get; set; }
        public string DisplayName { get; set; }
        public string? Description { get; set; }
        public string Category { get; set; }
        public ActivityTraits Traits { get; set; }
        public string[] Outcomes { get; set; }
        public ActivityPropertyDescriptor[] Properties { get; set; }
    }
}