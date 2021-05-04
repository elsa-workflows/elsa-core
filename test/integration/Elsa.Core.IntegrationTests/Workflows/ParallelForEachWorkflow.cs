using System.Collections.Generic;
using System.Linq;
using Elsa.Activities.Console;
using Elsa.Activities.ControlFlow;
using Elsa.Activities.Signaling;
using Elsa.Builders;

namespace Elsa.Core.IntegrationTests.Workflows
{
    public class ParallelForEachWorkflow : IWorkflow
    {
        private readonly List<object> _items;

        public ParallelForEachWorkflow(IEnumerable<string> items)
        {
            _items = items.Cast<object>().ToList();
        }

        public void Build(IWorkflowBuilder builder)
        {
            builder
                .ParallelForEach(
                    _items,
                    iterate => iterate
                        .Then<WriteLine>(activity => activity.Set(x => x.Text, context => $"{context.Input}")).WithId("WriteLine")
                        .Then<SignalReceived>() /* Block workflow.*/
                        .WriteLine("Resumed"))
                .WriteLine("All iterations executing in parallel");
        }
    }
}