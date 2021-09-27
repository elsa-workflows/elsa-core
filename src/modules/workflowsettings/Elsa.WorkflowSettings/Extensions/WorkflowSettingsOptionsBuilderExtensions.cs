using Elsa.Options;
using Elsa.WorkflowSettings.Handlers;
using Elsa.WorkflowSettings.Persistence;
using Elsa.WorkflowSettings.Persistence.Decorators;
using Elsa.WorkflowSettings.Providers;
using Elsa.WorkflowSettings.Services;
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
                .AddNotificationHandlersFrom<LoadWorkflowSettingDisabledHandler>()
                .AddTransient<IWorkflowSettingsProvider, ConfigurationWorkflowSettingsProvider>()
                .AddTransient<IWorkflowSettingsProvider, DatabaseWorkflowSettingsProvider>();

            return elsaOptions;
        }
    }
}
