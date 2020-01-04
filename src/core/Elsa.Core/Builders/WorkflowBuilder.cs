using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.Activities.Containers;
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
        private readonly IServiceProvider serviceProvider;

        public WorkflowBuilder(
            IActivityResolver activityResolver,
            IIdGenerator idGenerator,
            IServiceProvider serviceProvider)
        {
            this.activityResolver = activityResolver;
            this.idGenerator = idGenerator;
            this.serviceProvider = serviceProvider;
            Id = idGenerator.Generate();
            Version = 1;
        }

        public string Id { get; private set; }
        public string? Name { get; private set; }
        public string? Description { get; private set; }
        public int Version { get; private set; }
        public bool IsSingleton { get; private set; }
        public ProcessPersistenceBehavior PersistenceBehavior { get; private set; }
        public bool DeleteCompletedInstances { get; private set; }
        public IActivity? Root { get; private set; }

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

        public IWorkflowBuilder WithPersistenceBehavior(ProcessPersistenceBehavior value)
        {
            PersistenceBehavior = value;
            return this;
        }

        public IWorkflowBuilder WithDeleteCompletedInstances(bool value)
        {
            DeleteCompletedInstances = value;
            return this;
        }

        public IWorkflowBuilder StartWith<T>(Action<IActivityConfigurator<T>>? setupActivity = default) where T : class, IActivity
        {
            var activityConfigurator = BuildActivity<T>(x =>
            {
                x.WithId("root");
                setupActivity?.Invoke(x);
            });

            Root = activityConfigurator.Build();
            return this;
        }

        public IWorkflowBuilder StartWith<T>(Action<T>? setup = default) where T : class, IActivity
        {
            var activity = activityResolver.ResolveActivity<T>();
            setup?.Invoke(activity);
            return this;
        }

        public IWorkflowBuilder StartWith(IActivity activity)
        {
            Root = activity;
            return this;
        }

        public IActivityConfigurator<T> BuildActivity<T>(Action<IActivityConfigurator<T>>? setupActivity = default) where T : class, IActivity
        {
            var activity = activityResolver.ResolveActivity<T>();
            var activityConfigurator = new ActivityConfigurator<T>(activity);

            setupActivity?.Invoke(activityConfigurator);
            return activityConfigurator;
        }

        public T BuildActivity<T, TActivity>() where T : class, IActivityConfigurator<TActivity> where TActivity : class, IActivity
        {
            return serviceProvider.GetRequiredService<T>();
        }

        public T BuildActivity<T>(Action<T>? setupActivity = default) where T : class, IActivity
        {
            var activity = activityResolver.ResolveActivity<T>();

            setupActivity?.Invoke(activity);
            return activity;
        }

        public Workflow Build()
        {
            var definitionId = !string.IsNullOrWhiteSpace(Id) ? Id : idGenerator.Generate();

            return new Workflow(definitionId, Version, IsSingleton, false, Name, Description, true, true, Root);
        }

        public Workflow Build(IWorkflow workflow)
        {
            WithId(workflow.GetType().Name);
            workflow.Build(this);
            return Build();
        }

        public Workflow Build(Type processType)
        {
            var typedProcess = (IWorkflow)ActivatorUtilities.GetServiceOrCreateInstance(serviceProvider, processType);
            return Build(typedProcess);
        }

        public Workflow Build<T>() where T : IWorkflow => Build(typeof(T));
    }
}