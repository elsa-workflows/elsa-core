using System;

namespace Elsa.Attributes
{
    public class OutletAttribute : Attribute
    {
        public OutletAttribute(string outcome)
        {
            Outcome = outcome;
        }
        
        public string Outcome { get; }
    }
}