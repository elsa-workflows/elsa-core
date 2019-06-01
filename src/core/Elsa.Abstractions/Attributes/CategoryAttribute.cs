using System;

namespace Elsa.Attributes
{
    public class CategoryAttribute : Attribute
    {
        public CategoryAttribute(string category)
        {
            Category = category;
        }
        
        public string Category { get; }
    }
}