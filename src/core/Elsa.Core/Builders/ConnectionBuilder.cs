using System;
using Elsa.Models;

namespace Elsa.Builders
{
    public class ConnectionBuilder : IConnectionBuilder
    {
        public IFlowchartBuilder FlowchartBuilder { get; }
        public Func<IActivityBuilder> Source { get; }
        public Func<IActivityBuilder> Target{ get; }
        public string? Outcome { get; }

        public ConnectionBuilder(IFlowchartBuilder flowchartBuilder, Func<IActivityBuilder> source, Func<IActivityBuilder> target, string? outcome = null)
        {
            Source = source;
            Target = target;
            FlowchartBuilder = flowchartBuilder;
            Outcome = outcome;
        }

        public ConnectionDefinition BuildConnection()
        {
            return new ConnectionDefinition(Source().Activity.Id, Target().Activity.Id, Outcome);
        }
    }
}