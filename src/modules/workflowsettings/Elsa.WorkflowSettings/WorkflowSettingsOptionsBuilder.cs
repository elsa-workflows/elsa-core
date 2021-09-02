using System;
using Elsa.Caching;
using Elsa.WorkflowSettings.Options;
using Elsa.WorkflowSettings.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elsa.WorkflowSettings
{
    public class WorkflowSettingsOptionsBuilder
    {
        public WorkflowSettingsOptionsBuilder(IServiceCollection services)
        {
            WorkflowSettingsOptions = new WorkflowSettingsOptions();
            Services = services;
            services.TryAddSingleton<ICacheSignal, CacheSignal>();
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
