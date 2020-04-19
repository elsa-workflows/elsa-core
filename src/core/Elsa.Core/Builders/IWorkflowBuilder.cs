using System;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Builders
{
    public interface IWorkflowBuilder : IBuilder
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
        IWorkflowBuilder WithDeleteCompletedInstances(bool value);
        IWorkflowBuilder WithPersistenceBehavior(WorkflowPersistenceBehavior value);
        T New<T>(Action<T>? setup = default, Action<ActivityBuilder>? branch = default) where T : class, IActivity;
        T New<T>(T activity, Action<ActivityBuilder>? branch = default) where T : class, IActivity;
        ActivityBuilder StartWith<T>(Action<T>? setup = default, Action<ActivityBuilder>? branch = default) where T : class, IActivity;
        ActivityBuilder StartWith<T>(T activity, Action<ActivityBuilder>? branch = default) where T : class, IActivity;
        ActivityBuilder Add<T>(Action<T>? setup = default, Action<ActivityBuilder>? branch = default) where T : class, IActivity;
        ActivityBuilder Add<T>(T activity, Action<ActivityBuilder>? branch = default) where T : class, IActivity;
        ConnectionBuilder Connect(ActivityBuilder source, ActivityBuilder target, string? outcome = default);
        ConnectionBuilder Connect(Func<ActivityBuilder> source, Func<ActivityBuilder> target, string? outcome = default);
        Workflow Build();
        Workflow Build(IWorkflow workflow);
        Workflow Build(Type workflowType);
        Workflow Build<T>() where T:IWorkflow;
    }
}