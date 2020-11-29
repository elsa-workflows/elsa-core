namespace Elsa.Metadata
{
    public class ActivityInfo
    {
        public ActivityInfo()
        {
            Type = "Activity";
            Properties = new ActivityPropertyInfo[0];
            Category = "Miscellaneous";
            Traits = ActivityTraits.Action;
            DisplayName = "Activity";
            Properties = new ActivityPropertyInfo[0];
            Outcomes = new string[0];
        }

        public string Type { get; set; }
        public string DisplayName { get; set; }
        public string? Description { get; set; }
        public string Category { get; set; }
        public ActivityTraits Traits { get; set; }
        public string[] Outcomes { get; set; }
        public ActivityPropertyInfo[] Properties { get; set; }
        public bool Browsable { get; set; }
    }
}