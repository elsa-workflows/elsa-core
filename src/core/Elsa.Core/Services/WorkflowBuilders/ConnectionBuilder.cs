using System;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Core.Services.WorkflowBuilders
{
    public class ConnectionBuilder : IConnectionBuilder
    {
        public IWorkflowBuilder WorkflowBuilder { get; }
        public Func<IActivityBuilder> Source { get; }
        public Func<IActivityBuilder> Target{ get; }
        public string Outcome { get; }

        public ConnectionBuilder(IWorkflowBuilder workflowBuilder, Func<IActivityBuilder> source, Func<IActivityBuilder> target, string outcome = null)
        {
            Source = source;
            Target = target;
            WorkflowBuilder = workflowBuilder;
            Outcome = outcome;
        }

        public ConnectionBlueprint BuildConnection()
        {
            return new ConnectionBlueprint(Source().Activity.Id, Target().Activity.Id, Outcome);
        }
    }
}