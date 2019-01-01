using System.Collections.Generic;
using System.Linq;
using Elsa.Activities;
using Elsa.Models;
using Elsa.Results;
using Microsoft.Extensions.Localization;

namespace Elsa.Handlers
{
    public class UnknownActivityHandler : ActivityHandler<UnknownActivity>
    {
        public UnknownActivityHandler(IStringLocalizer<UnknownActivityHandler> localizer)
        {
            T = localizer;
        }

        public IStringLocalizer<UnknownActivityHandler> T { get; }
        public override LocalizedString Category => T["System"];
        protected override IEnumerable<LocalizedString> GetEndpoints() => Enumerable.Empty<LocalizedString>();
        
        protected override ActivityExecutionResult OnExecute(UnknownActivity activity, WorkflowExecutionContext workflowContext)
        {
            return Fault($"Unknown activity: {activity.Name}, ID: {activity.Id}");
        }
    }
}