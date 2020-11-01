using System;

namespace Elsa.Builders
{
    public class ConnectionBuilder : IConnectionBuilder
    {
        public ICompositeActivityBuilder WorkflowBuilder { get; }
        public Func<IActivityBuilder> Source { get; }
        public Func<IActivityBuilder> Target { get; }
        public string Outcome { get; }

        public ConnectionBuilder(ICompositeActivityBuilder workflowBuilder, Func<IActivityBuilder> source, Func<IActivityBuilder> target, string outcome = OutcomeNames.Done)
        {
            Source = source;
            Target = target;
            WorkflowBuilder = workflowBuilder;
            Outcome = outcome;
        }
    }
}