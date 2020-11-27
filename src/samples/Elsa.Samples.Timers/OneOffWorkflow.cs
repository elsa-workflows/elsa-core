using Elsa.Activities.Console;
using Elsa.Activities.Timers;
using Elsa.Builders;
using NodaTime;

namespace Elsa.Samples.Timers
{
    /// <summary>
    /// A workflow that executes only once in the near future.
    /// </summary>
    public class OneOffWorkflow : IWorkflow
    {
        private readonly Instant _executeAt;

        public OneOffWorkflow(Instant executeAt)
        {
            _executeAt = executeAt;
        }
        
        public void Build(IWorkflowBuilder workflow)
        {
            workflow
                .StartAt(_executeAt)
                .WriteLine(context => $"Started at {context.GetService<IClock>().GetCurrentInstant()}. Next event happens 3 seconds from now.")
                .StartAt(_executeAt.Plus(Duration.FromSeconds(3)))
                .WriteLine(context => $"Follow-up occurred at {context.GetService<IClock>().GetCurrentInstant()}.");
        }
    }
}