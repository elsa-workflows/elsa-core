using System;

namespace Elsa.Models
{
    public class ActivityDefinitionPropertyValue
    {
        public Type Type { get; set; } = default!;
        public string Syntax { get; set; } = default!;
        public string Expression { get; set; } = default!;
    }
}