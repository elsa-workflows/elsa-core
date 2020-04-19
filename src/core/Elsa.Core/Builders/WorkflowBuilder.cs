using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Builders
{
    public class WorkflowBuilder : IWorkflowBuilder
    {
        private readonly IActivityResolver activityResolver;
        private readonly IIdGenerator idGenerator;
        private readonly IList<ActivityBuilder> activityBuilders;
        private readonly IList<ConnectionBuilder> connectionBuilders;

        public WorkflowBuilder(
            IActivityResolver activityResolver,
            IIdGenerator idGenerator,
            IServiceProvider serviceProvider)
        {
            this.activityResolver = activityResolver;
            this.idGenerator = idGenerator;
            ServiceProvider = serviceProvider;
            Id = idGenerator.Generate();
            Version = 1;
            activityBuilders = new List<ActivityBuilder>();
            connectionBuilders = new List<ConnectionBuilder>();
        }

        public IServiceProvider ServiceProvider { get; }
        public string Id { get; private set; }
        public string? Name { get; private set; }
        public string? Description { get; private set; }
        public int Version { get; private set; }
        public bool IsSingleton { get; private set; }
        public WorkflowPersistenceBehavior PersistenceBehavior { get; private set; }
        public bool DeleteCompletedInstances { get; private set; }

        public IWorkflowBuilder WithId(string value)
        {
            Id = value;
            return this;
        }

        public IWorkflowBuilder WithName(string? value)
        {
            Name = value;
            return this;
        }

        public IWorkflowBuilder WithDescription(string? value)
        {
            Description = value;
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

        public IWorkflowBuilder WithDeleteCompletedInstances(bool value)
        {
            DeleteCompletedInstances = value;
            return this;
        }

        public T BuildActivity<T>(Action<T>? setup = default) where T : class, IActivity
        {
            var activity = activityResolver.ResolveActivity<T>();

            setup?.Invoke(activity);
            return activity;
        }

        public Workflow Build()
        {
            var definitionId = !string.IsNullOrWhiteSpace(Id) ? Id : idGenerator.Generate();
            var activities = activityBuilders.Select(x => x.BuildActivity()).ToList();
            var connections = connectionBuilders.Select(x => x.BuildConnection()).ToList();
            
            var workflow = new Workflow(
                definitionId, 
                Version, 
                IsSingleton, 
                false, 
                Name, 
                Description, 
                true, 
                true,
                PersistenceBehavior,
                DeleteCompletedInstances,
                activities,
                connections);

            // Generate deterministic activity ids.
            var id = 1;

            foreach (var activity in workflow.Activities)
            {
                if (string.IsNullOrEmpty(activity.Id))
                    activity.Id = $"activity-{id++}";
            }
            
            return workflow;
        }
        
        public T New<T>(T activity, Action<ActivityBuilder>? branch = default) where T : class, IActivity
        {
            var activityBuilder = new ActivityBuilder(this, activity);
            branch?.Invoke(activityBuilder);
            return activity;
        }

        public T New<T>(Action<T>? setup, Action<ActivityBuilder>? branch = default) where T : class, IActivity
        {
            var activity = activityResolver.ResolveActivity<T>();
            setup?.Invoke(activity);
            return New(activity, branch);
        }

        public ActivityBuilder StartWith<T>(Action<T>? setup = default, Action<ActivityBuilder>? branch = default) where T : class, IActivity
        {
            var activity = New(setup, branch);
            return StartWith(activity, branch);
        }
        
        public ActivityBuilder StartWith<T>(T activity, Action<ActivityBuilder>? branch = default) where T : class, IActivity
        {
            return Add(activity, branch);
        }
        
        public ActivityBuilder Add<T>(Action<T>? setup = default, Action<ActivityBuilder>? branch = default) where T : class, IActivity
        {
            var activity = New(setup, branch);
            return Add(activity, branch);
        }
        
        public ActivityBuilder Add<T>(T activity, Action<ActivityBuilder>? branch = default) where T : class, IActivity
        {
            var activityBuilder = new ActivityBuilder(this, activity);
            branch?.Invoke(activityBuilder);
            activityBuilders.Add(activityBuilder);
            return activityBuilder;
        }
        
        public ConnectionBuilder Connect(
            ActivityBuilder source,
            ActivityBuilder target,
            string? outcome = default)
        {
            return Connect(() => source, () => target, outcome);
        }

        public ConnectionBuilder Connect(
            Func<ActivityBuilder> source,
            Func<ActivityBuilder> target,
            string? outcome = default)
        {
            var connectionBuilder = new ConnectionBuilder(this, source, target, outcome);

            connectionBuilders.Add(connectionBuilder);
            return connectionBuilder;
        }
        
        public ActivityBuilder Then<T>(Action<T>? setup = default, Action<ActivityBuilder>? branch = default) where T : class, IActivity => StartWith(setup, branch);
        public ActivityBuilder Then<T>(T activity, Action<ActivityBuilder>? branch = default) where T : class, IActivity => StartWith(activity, branch);
        
        public Workflow Build(IWorkflow workflow)
        {
            WithId(workflow.GetType().Name);
            workflow.Build(this);
            return Build();
        }

        public Workflow Build(Type workflowType)
        {
            var workflow = (IWorkflow)ActivatorUtilities.GetServiceOrCreateInstance(ServiceProvider, workflowType);
            return Build(workflow);
        }

        public Workflow Build<T>() where T : IWorkflow => Build(typeof(T));
    }
}