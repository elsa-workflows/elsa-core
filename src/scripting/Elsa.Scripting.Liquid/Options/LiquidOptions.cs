using System;
using System.Collections.Generic;
using Elsa.Scripting.Liquid.Services;

namespace Elsa.Scripting.Liquid.Options
{
    public class LiquidOptions
    {
        public Dictionary<string, Type> FilterRegistrations { get; }  = new();
        public IList<Action<LiquidParser>> ParserConfiguration { get; } = new List<Action<LiquidParser>>();
        public bool EnableConfigurationAccess { get; set; }
    }
}
