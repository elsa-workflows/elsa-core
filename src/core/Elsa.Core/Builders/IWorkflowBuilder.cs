using System;
using System.Collections.Generic;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Builders
{
    public interface IWorkflowBuilder
    {
        string Id { get; }
        string Name { get; }
        string Description { get; }
        int Version { get; }
        WorkflowPersistenceBehavior PersistenceBehavior { get; }
        bool DeleteCompletedInstances { get; }
        IServiceProvider ServiceProvider { get; }
        IWorkflowBuilder WithId(string value);
        IWorkflowBuilder WithName(string value);
        IWorkflowBuilder WithDescription(string value);
        IWorkflowBuilder WithVersion(int value);
        IWorkflowBuilder AsSingleton();
        IWorkflowBuilder AsTransient();
        IWorkflowBuilder StartWith<T>(Action<T>? setup = default) where T : class, IActivity;
        IWorkflowBuilder StartWith(IActivity activity);
        IWorkflowBuilder StartWith(IActivityBuilder activity);
        T BuildActivity<T>(Action<T>? setup = default) where T : class, IActivity;
        Workflow Build();
        Workflow Build(IWorkflow process);
        Workflow Build(Type workflowType);
        Workflow Build<T>() where T:IWorkflow;
    }
}