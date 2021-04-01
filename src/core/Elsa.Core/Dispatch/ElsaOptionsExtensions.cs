using System;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Dispatch
{
    public static class ElsaOptionsExtensions
    {
        public static ElsaOptions UseDispatcher<T>(this ElsaOptions options) where T : class, IWorkflowDefinitionDispatcher, IWorkflowInstanceDispatcher, IWorkflowDispatcher
        {
            options.Services.AddSingleton<T>();

            options
                .UseCorrelatingWorkflowDispatcher(sp => sp.GetRequiredService<T>())
                .UseWorkflowDefinitionDispatcher(sp => sp.GetRequiredService<T>())
                .UseWorkflowInstanceDispatcher(sp => sp.GetRequiredService<T>());

            return options;
        }

        public static ElsaOptions UseWorkflowDefinitionDispatcher(this ElsaOptions options, Func<IServiceProvider, IWorkflowDefinitionDispatcher> factory)
        {
            options.WorkflowDefinitionDispatcherFactory = factory;
            return options;
        }

        public static ElsaOptions UseWorkflowInstanceDispatcher(this ElsaOptions options, Func<IServiceProvider, IWorkflowInstanceDispatcher> factory)
        {
            options.WorkflowInstanceDispatcherFactory = factory;
            return options;
        }

        public static ElsaOptions UseCorrelatingWorkflowDispatcher(this ElsaOptions options, Func<IServiceProvider, IWorkflowDispatcher> factory)
        {
            options.CorrelatingWorkflowDispatcherFactory = factory;
            return options;
        }
    }
}