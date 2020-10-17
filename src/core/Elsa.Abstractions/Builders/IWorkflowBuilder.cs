using System;
using System.Collections.Generic;
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
        bool IsEnabled { get; }
        IServiceProvider ServiceProvider { get; }
        IWorkflowBuilder WithId(string value);
        IWorkflowBuilder WithName(string value);
        IWorkflowBuilder WithDescription(string value);
        IWorkflowBuilder WithVersion(int value);
        IWorkflowBuilder AsSingleton();
        IWorkflowBuilder AsTransient();
        IWorkflowBuilder WithDeleteCompletedInstances(bool value);
        IWorkflowBuilder WithPersistenceBehavior(WorkflowPersistenceBehavior value);
        IWorkflowBuilder Enable(bool value);

        IActivityBuilder New<T>(
            Action<IActivityBuilder>? branch = default,
            IDictionary<string, IActivityPropertyValueProvider>? propertyValueProviders = default)
            where T : class, IActivity;

        IActivityBuilder New<T>(
            Action<ISetupActivity<T>>? setup,
            Action<IActivityBuilder>? branch = default) where T : class, IActivity;

        IActivityBuilder StartWith<T>(
            Action<ISetupActivity<T>>? setup,
            Action<IActivityBuilder>? branch = default) where T : class, IActivity;

        IActivityBuilder StartWith<T>(Action<IActivityBuilder>? branch = default)
            where T : class, IActivity;

        IActivityBuilder StartWith<T>(Action<T> setup, Action<IActivityBuilder>? branch = default)
            where T : class, IActivity;

        IActivityBuilder Add<T>(
            Action<ISetupActivity<T>>? setup = default,
            Action<IActivityBuilder>? branch = default) where T : class, IActivity;

        IActivityBuilder Add<T>(
            Action<T> setup,
            Action<IActivityBuilder>? branch = default) where T : class, IActivity;

        IActivityBuilder Add<T>(
            Action<IActivityBuilder>? branch = default,
            IDictionary<string, IActivityPropertyValueProvider>? propertyValueProviders = default)
            where T : class, IActivity;

        IConnectionBuilder Connect(
            IActivityBuilder source,
            IActivityBuilder target,
            string outcome = OutcomeNames.Done);

        IWorkflowBlueprint Build();
        IWorkflowBlueprint Build(IWorkflow workflow);
        IWorkflowBlueprint Build(Type workflowType);
        IWorkflowBlueprint Build<T>() where T : IWorkflow;
    }
}