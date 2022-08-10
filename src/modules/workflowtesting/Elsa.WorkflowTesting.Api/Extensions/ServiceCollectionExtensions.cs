using Autofac;
using Elsa.Extensions;
using Elsa.Server.Api.Services;
using Elsa.WorkflowTesting.Api.Handlers;
using Elsa.WorkflowTesting.Api.Services;
using Elsa.WorkflowTesting.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.WorkflowTesting.Api.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static ContainerBuilder AddWorkflowTestingServices(this ContainerBuilder services)
        {
            return services
                .AddScoped<IWorkflowTestLaunchpad, WorkflowTestLaunchpad>()
                .AddScoped<IWorkflowTestService, WorkflowTestService>()
                .AddNotificationHandlersFrom<ActivityExecutionResultExecutedHandler>()
                .AddTransient<IServerFeatureProvider, WorkflowTestingServerFeatureProvider>();
        }
    }
}