using System;
using System.Collections.Generic;

namespace Elsa.Scripting.Liquid.Options
{
    public class LiquidOptions
    {
        public Dictionary<string, Type> FilterRegistrations { get; }  = new Dictionary<string, Type>();
    }
}
