using System;
using System.Collections.Generic;
using Elsa.Activities.Workflows.Workflow;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Builders
{
    public class WorkflowBuilder : CompositeActivityBuilder, IWorkflowBuilder
    {
        public WorkflowBuilder(IIdGenerator idGenerator, IActivityActivator activityActivator, IServiceProvider serviceProvider) : base(
            idGenerator,
            activityActivator,
            serviceProvider)
        {
            Version = 1;
            IsEnabled = true;
            Variables = new Variables();
            CustomAttributes = new Variables();

            ActivityType = typeof(Workflow);
            WorkflowBuilder = this;
            PropertyValueProviders = new Dictionary<string, IActivityPropertyValueProvider>();
        }
        
        public int Version { get; private set; }
        public string? TenantId { get; private set; }
        public bool IsSingleton { get; private set; }

        public Variables Variables { get; }
        public Variables CustomAttributes { get; }
        public WorkflowContextOptions? ContextOptions { get; private set; }
        public WorkflowPersistenceBehavior PersistenceBehavior { get; private set; }
        public bool DeleteCompletedInstances { get; private set; }
        public bool IsEnabled { get; private set; }

        public IWorkflowBuilder WithWorkflowDefinitionId(string? value)
        {
            ActivityId = value!;
            return this;
        }
        
        public IWorkflowBuilder ForTenantId(string? value)
        {
            TenantId = value;
            return this;
        }
        
        public IWorkflowBuilder WithDescription(string? value)
        {
            Description = value;
            return this;
        }

        public IWorkflowBuilder WithContextType<T>(WorkflowContextFidelity fidelity) => WithContextType(typeof(T), fidelity);

        public IWorkflowBuilder WithContextType(Type type, WorkflowContextFidelity fidelity = WorkflowContextFidelity.Burst) =>
            WithContextOptions(
                new WorkflowContextOptions
                {
                    ContextType = type,
                    ContextFidelity = fidelity
                });

        public IWorkflowBuilder WithContextOptions(WorkflowContextOptions value)
        {
            ContextOptions = value;
            return this;
        }

        public IWorkflowBuilder WithVersion(int value)
        {
            Version = value;
            return this;
        }

        public IWorkflowBuilder AsSingleton()
        {
            IsSingleton = true;
            return this;
        }

        public IWorkflowBuilder AsTransient()
        {
            IsSingleton = false;
            return this;
        }

        public IWorkflowBuilder WithPersistenceBehavior(WorkflowPersistenceBehavior value)
        {
            PersistenceBehavior = value;
            return this;
        }

        public IWorkflowBuilder Enable(bool value)
        {
            IsEnabled = value;
            return this;
        }

        public IWorkflowBuilder WithDeleteCompletedInstances(bool value)
        {
            DeleteCompletedInstances = value;
            return this;
        }

        public IWorkflowBuilder WithVariable(string name, object value)
        {
            Variables.Set(name, value);
            return this;
        }

        public IWorkflowBuilder WithCustomAttribute(string name, object value)
        {
            CustomAttributes.Set(name, value);
            return this;
        }

        public IWorkflowBuilder WithTenantId(string value)
        {
            TenantId = value;
            return this;
        }

        public IWorkflowBlueprint Build(IWorkflow workflow, string activityIdPrefix)
        {
            WithId(workflow.GetType().Name);
            workflow.Build(this);
            return BuildBlueprint(activityIdPrefix);
        }

        public IWorkflowBlueprint Build(Type workflowType, string activityIdPrefix)
        {
            var workflow = (IWorkflow)ActivatorUtilities.GetServiceOrCreateInstance(ServiceProvider, workflowType);
            return Build(workflow, activityIdPrefix);
        }

        public IWorkflowBlueprint Build<T>(string activityIdPrefix) where T : IWorkflow => Build(typeof(T), activityIdPrefix);

        public IWorkflowBlueprint BuildBlueprint(string activityIdPrefix)
        {
            var compositeRoot = Build(activityIdPrefix);

            return new WorkflowBlueprint(
                ActivityId,
                Version,
                TenantId,
                IsSingleton,
                IsEnabled,
                Name,
                Description,
                true,
                true,
                Variables,
                CustomAttributes,
                ContextOptions,
                PersistenceBehavior,
                DeleteCompletedInstances,
                compositeRoot.Activities,
                compositeRoot.Connections,
                compositeRoot.ActivityPropertyProviders);
        }
    }
}