using System;

namespace Elsa.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ActivityDescriptionAttribute : Attribute
    {
        public ActivityDescriptionAttribute(string description)
        {
            Description = description;
        }
        
        public string Description { get; }
    }
}