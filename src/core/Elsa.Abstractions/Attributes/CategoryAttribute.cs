using System;

namespace Elsa.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class CategoryAttribute : Attribute
    {
        public CategoryAttribute(string category)
        {
            Category = category;
        }

        public string Category { get; }
    }
}