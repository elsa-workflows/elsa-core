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
        public WorkflowBuilder(IIdGenerator idGenerator, IServiceProvider serviceProvider, IGetsStartActivities startingActivitiesProvider) : base(serviceProvider, startingActivitiesProvider)
        {
            Version = 1;
            IsLatest = true;
            IsPublished = true;
            Variables = new Variables();
            CustomAttributes = new Variables();
            ActivityId = idGenerator.Generate();
            ActivityType = typeof(Workflow);
            ActivityTypeName = nameof(Workflow);
            WorkflowBuilder = this;
            PropertyValueProviders = new Dictionary<string, IActivityPropertyValueProvider>();
            PersistenceBehavior = WorkflowPersistenceBehavior.WorkflowBurst;
        }
        
        public int Version { get; private set; }
        public bool IsLatest { get; private set; }
        public bool IsPublished { get; private set; }
        public string? TenantId { get; private set; }
        public bool IsSingleton { get; private set; }
        public string? Tag { get; private set; }
        public string? Channel { get; private set; }

        public Variables Variables { get; }
        public Variables CustomAttributes { get; }
        public WorkflowContextOptions? ContextOptions { get; private set; }
        public WorkflowPersistenceBehavior PersistenceBehavior { get; private set; }
        public bool DeleteCompletedInstances { get; private set; }

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
        
        public new IWorkflowBuilder WithDisplayName(string value)
        {
            DisplayName = value;
            return this;
        }
        
        public new IWorkflowBuilder WithDescription(string? value)
        {
            Description = value;
            return this;
        }
        
        public IWorkflowBuilder WithChannel(string? value)
        {
            Channel = value;
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

        public IWorkflowBuilder WithVersion(int value, bool isLatest = true, bool isPublished = true)
        {
            Version = value;
            IsLatest = isLatest;
            IsPublished = isPublished;
            return this;
        }
        
        public IWorkflowBuilder WithTag(string value)
        {
            Tag = value;
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
            var workflowTypeName = workflow.GetType().Name;

            Name ??= workflowTypeName;
            DisplayName ??= workflowTypeName;
            
            WithId(workflowTypeName);
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
                Name,
                DisplayName,
                Description,
                IsLatest,
                IsPublished,
                Tag,
                Channel,
                Variables,
                CustomAttributes,
                ContextOptions,
                PersistenceBehavior,
                DeleteCompletedInstances,
                compositeRoot.Activities,
                compositeRoot.Connections,
                compositeRoot.ActivityPropertyProviders);
        }

        // Do not qualify root activities.
        protected override string? GetCompositeName(string? activityName) => activityName;
    }
}