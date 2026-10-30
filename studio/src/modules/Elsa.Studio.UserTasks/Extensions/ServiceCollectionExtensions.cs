using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;
using Elsa.Studio.Models;
using Elsa.Studio.UserTasks.Client;
using Elsa.Studio.UserTasks.Contracts;
using Elsa.Studio.UserTasks.Menu;
using Elsa.Studio.UserTasks.Services;
using Elsa.Studio.Workflows.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.UserTasks.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUserTasksModule(this IServiceCollection services, BackendApiConfig backendApiConfig)
    {
        return services
            .AddScoped<IFeature, Feature>()
            .AddScoped<IWorkflowMenuContributor, UserTasksMenu>()
            .AddScoped<IUserTaskRealtimeClient, SignalRUserTaskRealtimeClient>()
            .AddScoped<IUserTaskPollingService, UserTaskPollingService>()
            .AddRemoteApi<IUserTasksApi>(backendApiConfig)
            .AddRemoteApi<IUserTaskGuestApi>(backendApiConfig);
    }
}
