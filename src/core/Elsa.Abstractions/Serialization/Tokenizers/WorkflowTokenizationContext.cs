using System.Collections.Generic;

namespace Elsa.Serialization.Tokenizers
{
    public class WorkflowTokenizationContext
    {
        public IDictionary<IActivity, string> ActivityIdLookup { get; set; }
        public IDictionary<string, IActivity> ActivityLookup { get; set; }
    }
}