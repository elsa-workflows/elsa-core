using System;

namespace Elsa.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ActivityCategoryAttribute : Attribute
    {
        public ActivityCategoryAttribute(string category)
        {
            Category = category;
        }

        public string Category { get; }
    }
}