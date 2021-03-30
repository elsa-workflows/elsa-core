using Elsa.Dispatch;
using Elsa.Server.Orleans.Dispatch;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Server.Orleans.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static ElsaOptions UseOrleansDispatchers(this ElsaOptions elsaOptions)
        {
            var services = elsaOptions.Services;

            services.AddSingleton<GrainWorkflowDispatcher>();

            elsaOptions
                .UseCorrelatingWorkflowDispatcher(sp => sp.GetRequiredService<GrainWorkflowDispatcher>())
                .UseWorkflowDefinitionDispatcher(sp => sp.GetRequiredService<GrainWorkflowDispatcher>())
                .UseWorkflowInstanceDispatcher(sp => sp.GetRequiredService<GrainWorkflowDispatcher>());

            return elsaOptions;
        }
    }
}