using System;
using Elsa.Models;
using Elsa.Services.Models;

namespace Elsa.Builders
{
    /// <summary>
    /// Constructs workflow blueprints from programmatic workflow definitions.
    /// </summary>
    public interface IWorkflowBuilder : ICompositeActivityBuilder
    {
        int Version { get; }
        WorkflowPersistenceBehavior PersistenceBehavior { get; }
        bool DeleteCompletedInstances { get; }
        bool IsEnabled { get; }
        IWorkflowBuilder WithWorkflowDefinitionId(string value);
        IWorkflowBuilder WithDescription(string value);
        IWorkflowBuilder WithVersion(int value);
        IWorkflowBuilder AsSingleton();
        IWorkflowBuilder AsTransient();
        IWorkflowBuilder WithContextType<T>(WorkflowContextFidelity fidelity = WorkflowContextFidelity.Burst);
        IWorkflowBuilder WithContextType(Type value, WorkflowContextFidelity fidelity = WorkflowContextFidelity.Burst);
        IWorkflowBuilder WithDeleteCompletedInstances(bool value);
        IWorkflowBuilder WithPersistenceBehavior(WorkflowPersistenceBehavior value);
        IWorkflowBuilder Enable(bool value);
        IWorkflowBuilder WithVariable(string name, object value);
        IWorkflowBuilder WithCustomAttribute(string name, object value);
        IWorkflowBuilder WithTenantId(string value);

        IWorkflowBlueprint BuildBlueprint(string activityIdPrefix = "activity");
        IWorkflowBlueprint Build(IWorkflow workflow, string activityIdPrefix = "activity");
        IWorkflowBlueprint Build(Type workflowType, string activityIdPrefix = "activity");
        IWorkflowBlueprint Build<T>(string activityIdPrefix = "activity") where T : IWorkflow;
    }
}