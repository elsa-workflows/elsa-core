using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Elsa.Builders;
using Elsa.Caching;
using Elsa.Persistence;
using Elsa.Providers.WorkflowStorage;
using Elsa.Services;
using Elsa.Services.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Rebus.DataBus.InMem;
using Rebus.Persistence.InMem;
using Rebus.Transport.InMem;

namespace Elsa
{
    public class ElsaOptionsBuilder
    {
        public ElsaOptionsBuilder(IServiceCollection services)
        {
            ElsaOptions = new ElsaOptions();
            Services = services;

            AddAutoMapper = () =>
            {
                // The profiles are added to AddWorkflowsCore so that they are not forgotten in case the AddAutoMapper function(option) is overridden.
                services.AddAutoMapper(Enumerable.Empty<Assembly>(), ServiceLifetime.Singleton);
            };

            services.AddSingleton<InMemNetwork>();
            services.AddSingleton<InMemorySubscriberStore>();
            services.AddSingleton<InMemDataStore>();
            services.AddMemoryCache();
            services.AddSingleton<ICacheSignal, CacheSignal>();

            DistributedLockingOptionsBuilder = new DistributedLockingOptionsBuilder(this);
        }

        public ElsaOptions ElsaOptions { get; }
        public IServiceCollection Services { get; }
        public DistributedLockingOptionsBuilder DistributedLockingOptionsBuilder { get; }
        internal Action AddAutoMapper { get; private set; }
        internal bool WithCoreActivities { get; set; } = true;

        public ElsaOptionsBuilder NoCoreActivities()
        {
            WithCoreActivities = false;
            return this;
        }

        public ElsaOptionsBuilder WithContainerName(string name)
        {
            ElsaOptions.ContainerName = name;
            return this;
        }

        public ElsaOptionsBuilder ConfigureWorkflowChannels(Action<WorkflowChannelOptions> configure)
        {
            ElsaOptions.WorkflowChannelOptions.Channels = new List<string>();
            configure(ElsaOptions.WorkflowChannelOptions);
            return this;
        }

        public ElsaOptionsBuilder AddActivity<T>() where T : IActivity => AddActivity(typeof(T));

        public ElsaOptionsBuilder AddActivity(Type activityType)
        {
            Services.AddTransient(activityType);
            Services.AddTransient(sp => (IActivity) sp.GetRequiredService(activityType));
            ElsaOptions.ActivityFactory.Add(activityType, provider => (IActivity) ActivatorUtilities.GetServiceOrCreateInstance(provider, activityType));
            return this;
        }

        public ElsaOptionsBuilder AddActivitiesFrom(Assembly assembly) => AddActivitiesFrom(new[] { assembly });
        public ElsaOptionsBuilder AddActivitiesFrom(params Assembly[] assemblies) => AddActivitiesFrom((IEnumerable<Assembly>) assemblies);
        public ElsaOptionsBuilder AddActivitiesFrom(params Type[] assemblyMarkerTypes) => AddActivitiesFrom(assemblyMarkerTypes.Select(x => x.Assembly).Distinct());
        public ElsaOptionsBuilder AddActivitiesFrom<TMarker>() where TMarker : class => AddActivitiesFrom(typeof(TMarker));

        public ElsaOptionsBuilder AddActivitiesFrom(IEnumerable<Assembly> assemblies)
        {
            var types = assemblies.SelectMany(x => x.GetAllWithInterface<IActivity>());

            foreach (var type in types)
                AddActivity(type);

            return this;
        }

        public ElsaOptionsBuilder RemoveActivity<T>() where T : IActivity => RemoveActivity(typeof(T));

        public ElsaOptionsBuilder RemoveActivity(Type activityType)
        {
            ElsaOptions.ActivityFactory.Remove(activityType);
            return this;
        }

        public ElsaOptionsBuilder AddWorkflow<T>() where T : IWorkflow => AddWorkflow(typeof(T));

        public ElsaOptionsBuilder AddWorkflow(Type workflowType)
        {
            var workflowFactory = ElsaOptions.WorkflowFactory;

            if (workflowFactory.Types.Contains(workflowType))
                return this;

            Services.AddSingleton(workflowType);
            Services.AddSingleton(sp => (IWorkflow) sp.GetRequiredService(workflowType));
            workflowFactory.Add(workflowType, provider => (IWorkflow) ActivatorUtilities.GetServiceOrCreateInstance(provider, workflowType));
            return this;
        }

        public ElsaOptionsBuilder AddWorkflow(IWorkflow workflow)
        {
            Services.AddSingleton(workflow);
            ElsaOptions.WorkflowFactory.Add(workflow.GetType(), workflow);
            return this;
        }

        public ElsaOptionsBuilder AddWorkflow<T>(Func<IServiceProvider, T> workflow) where T : class, IWorkflow
        {
            Services.AddSingleton(workflow);
            Services.AddSingleton<IWorkflow>(sp => sp.GetRequiredService<T>());
            ElsaOptions.WorkflowFactory.Add(typeof(T), sp => sp.GetRequiredService<T>());

            return this;
        }

        public ElsaOptionsBuilder AddWorkflowsFrom<T>() => AddWorkflowsFrom(typeof(T).Assembly);

        public ElsaOptionsBuilder AddWorkflowsFrom(Assembly assembly)
        {
            var types = assembly.GetAllWithInterface<IWorkflow>();

            foreach (var type in types)
                AddWorkflow(type);

            return this;
        }

        public ElsaOptionsBuilder RemoveWorkflow<T>() where T : IWorkflow => RemoveWorkflow(typeof(T));

        public ElsaOptionsBuilder RemoveWorkflow(Type workflowType)
        {
            ElsaOptions.WorkflowFactory.Remove(workflowType);
            return this;
        }

        public ElsaOptionsBuilder AddCompetingMessageType(Type messageType, string? queueName = default)
        {
            ElsaOptions.CompetingMessageTypes.Add(new MessageTypeConfig(messageType, queueName));
            return this;
        }

        public ElsaOptionsBuilder AddCompetingMessageType<T>(string? queueName = default) => AddCompetingMessageType(typeof(T), queueName);

        public ElsaOptionsBuilder AddPubSubMessageType(Type messageType, string? queueName = default)
        {
            ElsaOptions.PubSubMessageTypes.Add(new MessageTypeConfig(messageType, queueName));
            return this;
        }

        public ElsaOptionsBuilder AddPubSubMessageType<T>(string? queueName = default) => AddPubSubMessageType(typeof(T), queueName);

        public ElsaOptionsBuilder ConfigureDistributedLockProvider(Action<DistributedLockingOptionsBuilder> configureOptions)
        {
            configureOptions(DistributedLockingOptionsBuilder);
            return this;
        }

        public ElsaOptionsBuilder UseWorkflowDefinitionStore(Func<IServiceProvider, IWorkflowDefinitionStore> factory)
        {
            ElsaOptions.WorkflowDefinitionStoreFactory = factory;
            return this;
        }

        public ElsaOptionsBuilder UseWorkflowInstanceStore(Func<IServiceProvider, IWorkflowInstanceStore> factory)
        {
            ElsaOptions.WorkflowInstanceStoreFactory = factory;
            return this;
        }

        public ElsaOptionsBuilder UseWorkflowExecutionLogStore(Func<IServiceProvider, IWorkflowExecutionLogStore> factory)
        {
            ElsaOptions.WorkflowExecutionLogStoreFactory = factory;
            return this;
        }

        public ElsaOptionsBuilder UseWorkflowTriggerStore(Func<IServiceProvider, IBookmarkStore> factory)
        {
            ElsaOptions.WorkflowTriggerStoreFactory = factory;
            return this;
        }

        public ElsaOptionsBuilder UseAutoMapper(Action addAutoMapper)
        {
            AddAutoMapper = addAutoMapper;
            return this;
        }

        public ElsaOptionsBuilder UseJsonSerializer(Func<IServiceProvider, JsonSerializer> factory)
        {
            ElsaOptions.CreateJsonSerializer = factory;
            return this;
        }

        public ElsaOptionsBuilder ConfigureJsonSerializer(Action<IServiceProvider, JsonSerializer> configure)
        {
            ElsaOptions.JsonSerializerConfigurer = configure;
            return this;
        }

        public ElsaOptionsBuilder UseDefaultWorkflowStorageProvider<T>() where T : IWorkflowStorageProvider => UseDefaultWorkflowStorageProvider(typeof(T));

        public ElsaOptionsBuilder UseDefaultWorkflowStorageProvider(Type type)
        {
            ElsaOptions.DefaultWorkflowStorageProviderType = type;
            return this;
        }

        public ElsaOptionsBuilder UseServiceBus(Action<ServiceBusEndpointConfigurationContext> setup)
        {
            ElsaOptions.ConfigureServiceBusEndpoint = setup;
            return this;
        }

        public ElsaOptionsBuilder AddCustomTenantAccessor<T>() where T : class, ITenantAccessor
        {
            Services.AddScoped<ITenantAccessor, T>();
            return this;
        }
    }
}