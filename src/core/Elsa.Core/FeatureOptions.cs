using System.Collections.Generic;

namespace Elsa
{
    public class FeatureOptions
    {
        public bool Enabled { get; set; }
        public string? Framework { get; set; }
        public string? ConnectionStringIdentifier { get; set; }
        public Dictionary<string, string>? Options { get; set; }
    }
}
