using System;
using Elsa.Caching;
using Elsa.WorkflowSettings.Abstractions.Persistence;
using Elsa.WorkflowSettings.Options;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.WorkflowSettings
{
    public class WorkflowSettingsOptionsBuilder
    {
        public WorkflowSettingsOptionsBuilder(IServiceCollection services)
        {
            WorkflowSettingsOptions = new WorkflowSettingsOptions();
            Services = services;
            services.AddMemoryCache();
            services.AddSingleton<ICacheSignal, CacheSignal>();
        }

        public WorkflowSettingsOptions WorkflowSettingsOptions { get; }
        public IServiceCollection Services { get; }

        public WorkflowSettingsOptionsBuilder UseWorkflowSettingsStore(Func<IServiceProvider, IWorkflowSettingsStore> factory)
        {
            WorkflowSettingsOptions.WorkflowSettingsStoreFactory = factory;
            return this;
        }
    }
}
