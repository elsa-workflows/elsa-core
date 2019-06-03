using System;

namespace Elsa.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ActivityDisplayNameAttribute : Attribute
    {
        public ActivityDisplayNameAttribute(string displayName)
        {
            DisplayName = displayName;
        }
        
        public string DisplayName { get; }
    }
}