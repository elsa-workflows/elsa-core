using Elsa.WorkflowSettings.Abstractions.Persistence;
using Elsa.WorkflowSettings.Abstractions.Providers;
using Elsa.WorkflowSettings.Abstractions.Services.WorkflowSettings;
using Elsa.WorkflowSettings.Handlers;
using Elsa.WorkflowSettings.Persistence.Decorators;
using Elsa.WorkflowSettings.Providers;
using Elsa.WorkflowSettings.Services.WorkflowSettingsContexts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.WorkflowSettings.Extensions
{
    public static class WorkflowSettingsOptionsBuilderExtensions
    {
        public static ElsaOptionsBuilder AddWorkflowSettings(this ElsaOptionsBuilder elsaOptions)
        {
            elsaOptions.Services
                .AddScoped<IWorkflowSettingsManager, WorkflowSettingsManager>()
                .Decorate<IWorkflowSettingsStore, InitializingWorkflowSettingsStore>()
                .Decorate<IWorkflowSettingsStore, EventPublishingWorkflowSettingsStore>()
                .AddNotificationHandlersFrom<LoadWorkflowSettings>()
                .AddTransient<IWorkflowSettingsProvider, ConfigurationWorkflowSettingsProvider>()
                .AddTransient<IWorkflowSettingsProvider, DatabaseWorkflowSettingsProvider>();

            return elsaOptions;
        }
    }
}
