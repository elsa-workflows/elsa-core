using System;
using Elsa.Services.Models;

namespace Elsa.Builders
{
    public interface IConnectionBuilder
    {
        IWorkflowBuilder WorkflowBuilder { get; }
        Func<IActivityBuilder> Source { get; }
        Func<IActivityBuilder> Target{ get; }
        string Outcome { get; }
        //Connection BuildConnection();
    }
}