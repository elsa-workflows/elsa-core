using System;
using Elsa.Activities.Flowcharts;
using Elsa.Models;
using Elsa.Services.Models;

namespace Elsa.Builders
{
    public class ConnectionBuilder
    {
        public FlowchartConfigurator FlowchartConfigurator { get; }
        public IActivity Source { get; }
        public IActivity Target{ get; }
        public string? Outcome { get; }

        public ConnectionBuilder(FlowchartConfigurator flowchartConfigurator, IActivity source, IActivity target, string? outcome = null)
        {
            Source = source;
            Target = target;
            FlowchartConfigurator = flowchartConfigurator;
            Outcome = outcome;
        }

        public Connection BuildConnection()
        {
            return new Connection(Source, Target, Outcome);
        }
    }
}