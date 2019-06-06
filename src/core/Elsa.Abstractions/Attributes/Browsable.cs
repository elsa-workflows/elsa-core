using System;

namespace Elsa.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class BrowsableAttribute : Attribute
    {
        public BrowsableAttribute(bool isBrowsable = true)
        {
            IsBrowsable = isBrowsable;
        }

        public bool IsBrowsable { get; }
    }
}