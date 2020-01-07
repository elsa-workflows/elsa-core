using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.Extensions;
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
            Activities = new List<IActivity>();
            Connections = new List<ConnectionBuilder>();
        }

        public IServiceProvider ServiceProvider { get; }
        public string Id { get; private set; }
        public string? Name { get; private set; }
        public string? Description { get; private set; }
        public int Version { get; private set; }
        public bool IsSingleton { get; private set; }
        public WorkflowPersistenceBehavior PersistenceBehavior { get; private set; }
        public bool DeleteCompletedInstances { get; private set; }
        public ICollection<IActivity> Activities { get; set; }
        public ICollection<ConnectionBuilder> Connections { get; }

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
            var connections = Connections.Where(x => x.Connection.Target != null).Select(x => x.Connection).ToList();
            
            var workflow = new Workflow(
                definitionId, 
                Version, 
                IsSingleton, 
                false, 
                Name, 
                Description, 
                true, 
                true, 
                Activities,
                connections);

            // Generate deterministic activity ids.
            var id = 1;
            var activities = workflow?.SelectActivities().ToList();
            
            foreach (var activity in activities)
            {
                if (string.IsNullOrEmpty(activity.Id))
                    activity.Id = $"activity-{id++}";
            }
            
            return workflow;
        }

        public ConnectionBuilder StartWith<T>(Action<T>? setup = default) where T : class, IActivity
        {
            var activity = activityResolver.ResolveActivity<T>();
            setup?.Invoke(activity);
            return StartWith(activity);
        }
        
        public ConnectionBuilder StartWith<T>(T activity) where T : class, IActivity
        {
            return new ConnectionBuilder(this, activityResolver, ServiceProvider, activity);
        }
        
        public IWorkflowBuilder Add<T>(Action<T>? setup = default) where T : class, IActivity
        {
            var activity = activityResolver.ResolveActivity(setup);
            return Add(activity);
        }
        
        public IWorkflowBuilder Add<T>(T activity) where T : class, IActivity
        {
            Activities.Add(activity);
            return this;
        }
        
        public IWorkflowBuilder Add(ConnectionBuilder connectionBuilder)
        {
            Connections.Add(connectionBuilder);
            return this;
        }
        
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