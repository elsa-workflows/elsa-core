using System.Collections.Generic;
using Flowsharp.Models;

namespace Flowsharp.Serialization
{
    public class WorkflowTokenizationContext
    {
        public IDictionary<IActivity, int> ActivityIdLookup { get; set; }
        public IDictionary<int, IActivity> ActivityLookup { get; set; }
    }
}