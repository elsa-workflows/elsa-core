using Elsa.UserTasks.Contracts;
using Elsa.UserTasks.Options;
using Elsa.UserTasks.Repositories;
using Elsa.UserTasks.Services;
using Elsa.Mediator.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elsa.UserTasks.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUserTasksServices(this IServiceCollection services, Action<UserTasksOptions>? configure = null)
    {
        if (configure != null)
            services.Configure(configure);
        services.AddOptions<UserTasksOptions>();
        services.TryAddSingleton<IUserTaskIdentityResolver, DefaultClaimsIdentityResolver>();
        services.TryAddSingleton<IUserTaskAccessPolicy, DefaultUserTaskAccessPolicy>();
        services.TryAddSingleton<IUserTaskParticipantDirectory, EmptyUserTaskParticipantDirectory>();
        services.TryAddSingleton<IUserTaskRepository, InMemoryUserTaskRepository>();
        services.TryAddSingleton<IUserTaskNotificationSink, DefaultUserTaskNotificationSink>();
        services.TryAddSingleton<IUserTaskWorkflowResumer, DefaultUserTaskWorkflowResumer>();
        services.TryAddSingleton<IUserTaskManager, DefaultUserTaskManager>();
        services.TryAddSingleton<IUserTaskProjectionService, DefaultUserTaskProjectionService>();
        services.TryAddSingleton<IUserTaskDueService, DefaultUserTaskDueService>();
        // Bookmark stores are commonly scoped by the runtime/persistence module; keep the reconciler
        // scoped so hosts can resolve it from a bounded worker scope without promoting that dependency.
        services.TryAddScoped<IUserTaskReconciler, DefaultUserTaskReconciler>();
        services.TryAddSingleton<IUserTaskInvitationService, DefaultUserTaskInvitationService>();
        services.TryAddSingleton<IUserTaskInvitationDispatcher, DefaultUserTaskInvitationDispatcher>();
        services.TryAddSingleton<IUserTaskInvitationVerifier, DefaultUserTaskInvitationVerifier>();
        services.TryAddSingleton<IUserTaskGuestSessionIssuer, DefaultUserTaskGuestSessionIssuer>();
        services.AddNotificationHandler<Handlers.UserTaskBookmarkPersistedHandler>();
        return services;
    }
}
