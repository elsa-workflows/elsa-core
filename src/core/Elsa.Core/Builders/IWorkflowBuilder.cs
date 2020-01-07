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
        ConnectionBuilder StartWith<T>(Action<T>? setup = default) where T : class, IActivity;
        ConnectionBuilder StartWith<T>(T activity) where T : class, IActivity;
        IWorkflowBuilder Add<T>(Action<T>? setup = default) where T : class, IActivity;
        IWorkflowBuilder Add<T>(T activity) where T : class, IActivity;
        IWorkflowBuilder Add(ConnectionBuilder connectionBuilder);
        T BuildActivity<T>(Action<T>? setup = default) where T : class, IActivity;
        Workflow Build();
        Workflow Build(IWorkflow workflow);
        Workflow Build(Type workflowType);
        Workflow Build<T>() where T:IWorkflow;
    }
}