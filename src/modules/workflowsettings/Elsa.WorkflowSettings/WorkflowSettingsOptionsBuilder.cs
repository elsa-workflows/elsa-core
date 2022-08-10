using System;
using Autofac;
using AutoMapper;
using Elsa.Caching;
using Elsa.WorkflowSettings.Options;
using Elsa.WorkflowSettings.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elsa.WorkflowSettings
{
    public class WorkflowSettingsOptionsBuilder
    {
        public WorkflowSettingsOptionsBuilder(IServiceCollection services, ContainerBuilder containerBuilder)
        {
            WorkflowSettingsOptions = new WorkflowSettingsOptions();
            Services = services;
            ContainerBuilder = containerBuilder;
            services.TryAddSingleton<ICacheSignal, CacheSignal>();
        }

        public WorkflowSettingsOptions WorkflowSettingsOptions { get; }
        public IServiceCollection Services { get; }
        public ContainerBuilder ContainerBuilder { get; }

        public WorkflowSettingsOptionsBuilder UseWorkflowSettingsStore(Func<IServiceProvider, IWorkflowSettingsStore> factory)
        {
            WorkflowSettingsOptions.WorkflowSettingsStoreFactory = factory;
            return this;
        }
    }
}
