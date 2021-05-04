using System;

namespace Elsa.Builders
{
    public interface IConnectionBuilder
    {
        ICompositeActivityBuilder WorkflowBuilder { get; }
        Func<IActivityBuilder> Source { get; }
        Func<IActivityBuilder> Target{ get; }
        string Outcome { get; }
    }
}