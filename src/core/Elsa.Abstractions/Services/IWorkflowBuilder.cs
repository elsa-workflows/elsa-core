using System;
using Elsa.Serialization.Models;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public interface IWorkflowBuilder
    {
        IActivityBuilder Add<T>(Action<T> setupActivity) where T : IActivity;
        IActivityBuilder StartWith<T>(Action<T> setup = null) where T: IActivity;
        IConnectionBuilder Connect(IActivityBuilder source, IActivityBuilder target, string outcome = null);
        Workflow Build();
        Workflow Build(WorkflowDefinition definition);
    }
}