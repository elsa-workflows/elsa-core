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
            InputProperties = new ActivityInputDescriptor[0];
            OutputProperties = new ActivityOutputDescriptor[0];
            Outcomes = new string[0];
        }

        public string Type { get; set; }
        public string DisplayName { get; set; }
        public string? Description { get; set; }
        public string Category { get; set; }
        public ActivityTraits Traits { get; set; }
        public string[] Outcomes { get; set; }
        public ActivityInputDescriptor[] InputProperties { get; set; }
        public ActivityOutputDescriptor[] OutputProperties { get; set; }
    }
}