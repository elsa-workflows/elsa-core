using System;
using Elsa.Services.Models;

namespace Elsa.Builders
{
    public class ConnectionBuilder : IConnectionBuilder
    {
        public IWorkflowBuilder WorkflowBuilder { get; }
        public IActivityBuilder Source { get; }
        public IActivityBuilder Target{ get; }
        public string Outcome { get; }
        
        public ConnectionBuilder(IWorkflowBuilder workflowBuilder, IActivityBuilder source, IActivityBuilder target, string outcome = OutcomeNames.Done)
        {
            Source = source;
            Target = target;
            WorkflowBuilder = workflowBuilder;
            Outcome = outcome;
        }
    }
}