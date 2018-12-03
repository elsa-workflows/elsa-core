using System.Collections.Generic;
using Flowsharp.Models;

namespace Flowsharp.Serialization.Tokenizers
{
    public class WorkflowTokenizationContext
    {
        public IDictionary<IActivity, string> ActivityIdLookup { get; set; }
        public IDictionary<string, IActivity> ActivityLookup { get; set; }
    }
}