using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Console.Activities;
using Elsa.Handlers;
using Elsa.Models;
using Elsa.Results;
using Microsoft.Extensions.Localization;

namespace Elsa.Activities.Console.Handlers
{
    public class ReadLineHandler : ActivityHandler<ReadLine>
    {
        private readonly TextReader input;

        public ReadLineHandler(IStringLocalizer<ReadLineHandler> localizer)
        {
            T = localizer;
        }

        public ReadLineHandler(IStringLocalizer<ReadLineHandler> localizer, TextReader input) : this(localizer)
        {
            this.input = input;
        }

        private IStringLocalizer<ReadLineHandler> T { get; }

        protected override LocalizedString GetEndpoint() => T["Done"];

        public override LocalizedString Category => T["Console"];
        public override LocalizedString DisplayText => T["Read Line"];
        public override LocalizedString Description => T["Read a line from the console"];

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(ReadLine activity, WorkflowExecutionContext workflowContext, CancellationToken cancellationToken)
        {
            if (input == null)
                return Halt();

            var value = await input.ReadLineAsync();
            workflowContext.SetLastResult(value);
            return TriggerEndpoint("Done");
        }

        protected override ActivityExecutionResult OnResume(ReadLine activity, WorkflowExecutionContext workflowContext)
        {
            var receivedInput = workflowContext.Workflow.Arguments[activity.ArgumentName];
            workflowContext.SetLastResult(receivedInput);
            return TriggerEndpoint("Done");
        }
    }
}