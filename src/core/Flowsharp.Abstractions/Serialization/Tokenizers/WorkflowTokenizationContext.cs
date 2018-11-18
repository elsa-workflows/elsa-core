using System.Collections.Generic;
using Flowsharp.Models;

namespace Flowsharp.Serialization.Tokenizers
{
    public class WorkflowTokenizationContext
    {
        public IDictionary<IActivity, int> ActivityIdLookup { get; set; }
        public IDictionary<int, IActivity> ActivityLookup { get; set; }
    }
}