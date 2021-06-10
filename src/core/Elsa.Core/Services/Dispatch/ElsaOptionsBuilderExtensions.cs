using System;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Services.Dispatch
{
    public static class ElsaOptionsBuilderExtensions
    {
        public static ElsaOptionsBuilder UseDispatcher<T>(this ElsaOptionsBuilder options) where T : class, IWorkflowDefinitionDispatcher, IWorkflowInstanceDispatcher, IWorkflowDispatcher
        {
            options.Services.AddSingleton<T>();

            options
                .UseCorrelatingWorkflowDispatcher(sp => sp.GetRequiredService<T>())
                .UseWorkflowDefinitionDispatcher(sp => sp.GetRequiredService<T>())
                .UseWorkflowInstanceDispatcher(sp => sp.GetRequiredService<T>());

            return options;
        }

        public static ElsaOptionsBuilder UseWorkflowDefinitionDispatcher(this ElsaOptionsBuilder options, Func<IServiceProvider, IWorkflowDefinitionDispatcher> factory)
        {
            options.ElsaOptions.WorkflowDefinitionDispatcherFactory = factory;
            return options;
        }

        public static ElsaOptionsBuilder UseWorkflowInstanceDispatcher(this ElsaOptionsBuilder options, Func<IServiceProvider, IWorkflowInstanceDispatcher> factory)
        {
            options.ElsaOptions.WorkflowInstanceDispatcherFactory = factory;
            return options;
        }

        public static ElsaOptionsBuilder UseCorrelatingWorkflowDispatcher(this ElsaOptionsBuilder options, Func<IServiceProvider, IWorkflowDispatcher> factory)
        {
            options.ElsaOptions.CorrelatingWorkflowDispatcherFactory = factory;
            return options;
        }
    }
}