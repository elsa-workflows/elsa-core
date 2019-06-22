using Elsa.Services.Models;

namespace Elsa.Core.Services.WorkflowBuilders
{
    public class ConnectionBuilder : Builder
    {
        public ActivityBuilder Source { get; }
        public ActivityBuilder Target { get; }
        public string Outcome { get; }

        public ConnectionBuilder(WorkflowBuilder workflowBuilder, ActivityBuilder source, ActivityBuilder target, string outcome = null) : base(workflowBuilder)
        {
            Source = source;
            Target = target;
            Outcome = outcome;
        }

        public Connection BuildConnection()
        {
            return new Connection
            {
                Source = new SourceEndpoint(Source.Activity, Outcome),
                Target = new TargetEndpoint(Target.Activity)
            };
        }
    }
}