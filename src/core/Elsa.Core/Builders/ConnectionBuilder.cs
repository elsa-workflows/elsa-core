using System;
using Elsa.Services.Models;

namespace Elsa.Builders
{
    public class ConnectionBuilder
    {
        public IWorkflowBuilder WorkflowBuilder { get; }
        public Func<ActivityBuilder> Source { get; }
        public Func<ActivityBuilder> Target{ get; }
        public string Outcome { get; }

        public ConnectionBuilder(IWorkflowBuilder workflowBuilder, Func<ActivityBuilder> source, Func<ActivityBuilder> target, string? outcome = null)
        {
            Source = source;
            Target = target;
            WorkflowBuilder = workflowBuilder;
            Outcome = outcome;
        }

        public Connection BuildConnection()
        {
            return new Connection(Source().Activity, Target().Activity, Outcome);
        }
    }
}