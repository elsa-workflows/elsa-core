using System.Collections.Generic;

namespace Elsa
{
    public class FeatureOptions
    {
        public bool Enabled { get; set; }
        public Dictionary<string, string>? Items { get; set; }
        public Dictionary<string, string>? Options { get; set; }
    }
}
