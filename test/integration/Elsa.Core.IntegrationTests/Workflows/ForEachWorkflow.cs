using System.Collections.Generic;
using System.Linq;
using Elsa.Activities.Console;
using Elsa.Activities.ControlFlow;
using Elsa.Builders;

namespace Elsa.Core.IntegrationTests.Workflows
{
    public class ForEachWorkflow : IWorkflow
    {
        private readonly List<object> _items;

        public ForEachWorkflow(IEnumerable<string> items)
        {
            _items = items.Cast<object>().ToList();
        }

        public void Build(IWorkflowBuilder builder)
        {
            builder
                .ForEach(
                    _items,
                    iterate => iterate
                        .Then<WriteLine>(activity => activity.WithText(context => $"{context.Input}")).WithId("WriteLine")
                        .SignalReceived("The Signal") /* Block workflow.*/
                        .WriteLine("Resumed"))
                .WriteLine("One iterations executing, rest is blocked");
        }
    }
}