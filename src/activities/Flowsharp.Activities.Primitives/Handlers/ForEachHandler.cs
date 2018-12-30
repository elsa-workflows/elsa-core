using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Activities.Primitives.Activities;
using Flowsharp.Handlers;
using Flowsharp.Models;
using Flowsharp.Results;
using Microsoft.Extensions.Localization;

namespace Flowsharp.Activities.Primitives.Handlers
{
    public class ForEachHandler : ActivityHandler<ForEach>
    {
        private readonly IWorkflowExpressionEvaluator expressionEvaluator;

        public ForEachHandler(IStringLocalizer<ForEachHandler> localizer, IWorkflowExpressionEvaluator expressionEvaluator)
        {
            T = localizer;
            this.expressionEvaluator = expressionEvaluator;
        }

        public override LocalizedString Category => T["Control Flow"];
        public override LocalizedString DisplayText => T["For Each"];
        public override LocalizedString Description => T["Iterate over a list of items."];
        public IStringLocalizer<ForEachHandler> T { get; }

        public override IEnumerable<LocalizedString> GetEndpoints() => Endpoints(T["Next"], T["Done"]);

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(ForEach activity, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken)
        {
            return TriggerEndpoint("Done");
        }
    }
}