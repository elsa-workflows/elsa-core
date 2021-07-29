using Elsa.WorkflowSettings.Abstractions.Persistence;
using Elsa.WorkflowSettings.Abstractions.Services.WorkflowSettingsContexts;
using Elsa.WorkflowSettings.Handlers;
using Elsa.WorkflowSettings.Persistence.Decorators;
using Elsa.WorkflowSettings.Services.WorkflowSettingsContexts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.WorkflowSettings.Extensions
{
    public static class WorkflowSettingsOptionsBuilderExtensions
    {
        public static ElsaOptionsBuilder AddWorkflowSettings(this ElsaOptionsBuilder elsaOptions)
        {
            elsaOptions.Services
                .AddScoped<IWorkflowSettingsContextManager, WorkflowSettingsContextManager>()
                .Decorate<IWorkflowSettingsStore, InitializingWorkflowSettingsStore>()
                .Decorate<IWorkflowSettingsStore, EventPublishingWorkflowSettingsStore>()
                .AddNotificationHandlersFrom<LoadWorkflowSettings>();

            return elsaOptions;
        }
    }
}
