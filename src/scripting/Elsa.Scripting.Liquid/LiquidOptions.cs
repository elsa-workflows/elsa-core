using System;
using System.Collections.Generic;

namespace Elsa.Scripting.Liquid
{
    public class LiquidOptions
    {
        public Dictionary<string, Type> FilterRegistrations { get; }  = new Dictionary<string, Type>();
    }
}
