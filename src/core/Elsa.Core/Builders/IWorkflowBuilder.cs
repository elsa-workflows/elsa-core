using System;
using System.Collections.Generic;
using Elsa.Models;
using Elsa.Services.Models;

namespace Elsa.Builders
{
    public interface IWorkflowBuilder
    {
        string Id { get; }
        string Name { get; }
        string Description { get; }
        int Version { get; }
        ProcessPersistenceBehavior PersistenceBehavior { get; }
        bool DeleteCompletedInstances { get; }
        IWorkflowBuilder WithId(string value);
        IWorkflowBuilder WithName(string value);
        IWorkflowBuilder WithDescription(string value);
        IWorkflowBuilder WithVersion(int value);
        IWorkflowBuilder AsSingleton();
        IWorkflowBuilder AsTransient();
        IWorkflowBuilder StartWith<T>(Action<IActivityConfigurator<T>>? setup = default) where T : class, IActivity;
        IActivityConfigurator<T> BuildActivity<T>(Action<IActivityConfigurator<T>>? setupActivity) where T : class, IActivity;
        T BuildActivity<T, TActivity>() where T : class, IActivityConfigurator<TActivity> where TActivity : class, IActivity;
        T BuildActivity<T>(Action<T>? setupActivity = default) where T : class, IActivity;
        Workflow Build();
        Workflow Build(IWorkflow process);
        Workflow Build(Type processType);
        Workflow Build<T>() where T:IWorkflow;
        IWorkflowBuilder StartWith<T>(Action<T>? setup = default) where T : class, IActivity;
        IWorkflowBuilder StartWith(IActivity activity);
    }
}