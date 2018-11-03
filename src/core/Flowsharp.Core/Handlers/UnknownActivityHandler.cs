using System.Collections.Generic;
using System.Linq;
using Flowsharp.Activities;
using Flowsharp.Models;
using Flowsharp.Results;
using Microsoft.Extensions.Localization;

namespace Flowsharp.Handlers
{
    public class UnknownActivityHandler : ActivityHandler<UnknownActivity>
    {
        public override IEnumerable<LocalizedString> GetEndpoints() => Enumerable.Empty<LocalizedString>();
        
        protected override ActivityExecutionResult OnExecute(UnknownActivity activity, WorkflowExecutionContext workflowContext)
        {
            return Fault($"Unknown activity: {activity.Name}, ID: {activity.Id}");
        }
    }
}