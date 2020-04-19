using System;
using Elsa.Services.Models;

namespace Elsa.Builders
{
    public class ConnectionBuilder : IConnectionBuilder
    {
        public IWorkflowBuilder WorkflowBuilder { get; }
        public Func<IActivityBuilder> Source { get; }
        public Func<IActivityBuilder> Target{ get; }
        public string Outcome { get; }

        public ConnectionBuilder(IWorkflowBuilder workflowBuilder, Func<IActivityBuilder> source, Func<IActivityBuilder> target, string outcome = OutcomeNames.Done)
        {
            Source = source;
            Target = target;
            WorkflowBuilder = workflowBuilder;
            Outcome = outcome;
        }

        public Connection BuildConnection() => new Connection(Source().Activity, Target().Activity, Outcome);
    }
}