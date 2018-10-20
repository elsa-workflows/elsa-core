using System.Collections.Generic;
using Flowsharp.Activities;

namespace Flowsharp.Serialization.Tokenizers
{
    public class WorkflowTokenizationContext
    {
        public IDictionary<IActivity, int> ActivityIdLookup { get; set; }
        public IDictionary<int, IActivity> ActivityLookup { get; set; }
    }
}