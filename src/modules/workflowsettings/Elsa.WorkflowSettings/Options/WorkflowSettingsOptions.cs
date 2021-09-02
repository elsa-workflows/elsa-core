using System;
using Elsa.WorkflowSettings.Persistence;
using Elsa.WorkflowSettings.Persistence.InMemory;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.WorkflowSettings.Options
{
    public class WorkflowSettingsOptions
    {
        public WorkflowSettingsOptions()
        {
            WorkflowSettingsStoreFactory = provider => ActivatorUtilities.CreateInstance<InMemoryWorkflowSettingsStore>(provider);
        }
        public Func<IServiceProvider, IWorkflowSettingsStore> WorkflowSettingsStoreFactory { get; set; }
    }
}
