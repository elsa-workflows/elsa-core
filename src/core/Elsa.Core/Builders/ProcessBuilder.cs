using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.Activities.Containers;
using Elsa.Activities.Workflows.Models;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Builders
{
    public class ProcessBuilder : IProcessBuilder
    {
        private readonly IActivityResolver activityResolver;
        private readonly IIdGenerator idGenerator;
        private readonly IServiceProvider serviceProvider;

        public ProcessBuilder(
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

        public IProcessBuilder WithId(string value)
        {
            Id = value;
            return this;
        }

        public IProcessBuilder WithName(string? value)
        {
            Name = value;
            return this;
        }

        public IProcessBuilder WithDescription(string? value)
        {
            Description = value;
            return this;
        }

        public IProcessBuilder WithVersion(int value)
        {
            Version = value;
            return this;
        }

        public IProcessBuilder AsSingleton()
        {
            IsSingleton = true;
            return this;
        }

        public IProcessBuilder AsTransient()
        {
            IsSingleton = false;
            return this;
        }

        public IProcessBuilder WithPersistenceBehavior(ProcessPersistenceBehavior value)
        {
            PersistenceBehavior = value;
            return this;
        }

        public IProcessBuilder WithDeleteCompletedInstances(bool value)
        {
            DeleteCompletedInstances = value;
            return this;
        }

        public IProcessBuilder WithRoot<T>(Action<IActivityConfigurator<T>>? setupActivity = default) where T : class, IActivity
        {
            var activityConfigurator = BuildActivity<T>(x =>
            {
                x.WithId("root");
                setupActivity?.Invoke(x);
            });

            Root = activityConfigurator.Build();
            return this;
        }

        public IActivityConfigurator<T> BuildActivity<T>(Action<IActivityConfigurator<T>>? setupActivity = default) where T : class, IActivity
        {
            var activity = activityResolver.ResolveActivity<T>();
            var activityConfigurator = new ActivityConfigurator<T>(activity);

            setupActivity?.Invoke(activityConfigurator);
            return activityConfigurator;
        }

        public T BuildActivity<T>(Action<T>? setupActivity = default) where T : class, IActivity
        {
            var activity = activityResolver.ResolveActivity<T>();

            setupActivity?.Invoke(activity);
            return activity;
        }

        public Process Build()
        {
            var definitionId = !string.IsNullOrWhiteSpace(Id) ? Id : idGenerator.Generate();

            return new Process(definitionId, Version, IsSingleton, false, Name, Description, true, true, Root);
        }

        public Process Build(IProcess process)
        {
            WithId(process.GetType().Name);
            process.Build(this);
            return Build();
        }

        public Process Build(Type processType)
        {
            var typedProcess = (IProcess)ActivatorUtilities.GetServiceOrCreateInstance(serviceProvider, processType);
            return Build(typedProcess);
        }

        public Process Build<T>() where T : IProcess => Build(typeof(T));
    }
}