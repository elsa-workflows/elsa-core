using System;

namespace Elsa.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class IsTriggerAttribute : Attribute
    {
        public IsTriggerAttribute(bool isTrigger = true)
        {
            IsTrigger = isTrigger;
        }
        
        public bool IsTrigger { get; }
    }
}