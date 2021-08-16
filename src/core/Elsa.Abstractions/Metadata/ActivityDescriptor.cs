using System;

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
            InputProperties = Array.Empty<ActivityInputDescriptor>();
            OutputProperties = Array.Empty<ActivityOutputDescriptor>();
            Outcomes = Array.Empty<string>();
        }

        public string Type { get; set; }
        public string DisplayName { get; set; }
        public string? Description { get; set; }
        public string Category { get; set; }
        public ActivityTraits Traits { get; set; }
        public string[] Outcomes { get; set; }
        
        [Obsolete("Use InputProperties instead.")]
        public ActivityInputDescriptor[] Properties 
        { 
            get => InputProperties;
            set => InputProperties = value;
        }
        
        public ActivityInputDescriptor[] InputProperties { get; set; }
        public ActivityOutputDescriptor[] OutputProperties { get; set; }
    }
}