using System.Collections.Generic;

namespace Elsa.Models
{
    public class FeatureOption
    {
        public string Name { get; set; } = default!;
        public bool Enabled { get; set; } = default!;
        public Dictionary<string, string>? Options { get; set; }
    }
}
