using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Core.Services.WorkflowBuilders
{
    public class ConnectionBuilder : IConnectionBuilder
    {
        public IActivityBuilder Source { get; }
        public IActivityBuilder Target { get; }
        public string Outcome { get; }

        public ConnectionBuilder(IActivityBuilder source, IActivityBuilder target, string outcome = null)
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