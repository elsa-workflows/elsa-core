using Elsa.Activities.Console;
using Elsa.Activities.Temporal;
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
        
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .StartAt(_executeAt)
                .WriteLine(context => $"Started at {context.GetService<IClock>().GetCurrentInstant()}. Next event happens 30 seconds from now.")
                .StartIn(Duration.FromSeconds(30))
                .WriteLine(context => $"Follow-up occurred at {context.GetService<IClock>().GetCurrentInstant()}.");
        }
    }
}