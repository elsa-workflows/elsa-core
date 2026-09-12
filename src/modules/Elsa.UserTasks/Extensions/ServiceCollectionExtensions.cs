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

        // The in-memory defaults below hold process state, so they stay singletons. A persistence provider
        // replaces them with scoped, store-backed implementations.
        services.TryAddSingleton<IUserTaskRepository, InMemoryUserTaskRepository>();
        services.TryAddSingleton<IUserTaskGuestSessionIssuer, InMemoryUserTaskGuestSessionIssuer>();
        services.TryAddSingleton<IUserTaskInvitationRateLimiter, SlidingWindowUserTaskInvitationRateLimiter>();
        services.TryAddSingleton<IUserTaskInvitationOutbox, InMemoryUserTaskInvitationOutbox>();
        services.TryAddSingleton<IUserTaskInvitationDispatcher, NullUserTaskInvitationDispatcher>();
        services.TryAddSingleton<IUserTaskInvitationVerifier, DefaultUserTaskInvitationVerifier>();
        services.TryAddSingleton<IUserTaskNotificationSink, DefaultUserTaskNotificationSink>();
        services.TryAddSingleton<IUserTaskWorkflowResumer, DefaultUserTaskWorkflowResumer>();

        // Everything that consumes a repository is scoped: a provider-backed repository resolves a DbContext
        // from the ambient scope, and a singleton consumer would capture it.
        services.TryAddScoped<IUserTaskManager, DefaultUserTaskManager>();
        services.TryAddScoped<IUserTaskProjectionService, DefaultUserTaskProjectionService>();
        services.TryAddScoped<IUserTaskDueService, DefaultUserTaskDueService>();
        services.TryAddScoped<IUserTaskReconciler, DefaultUserTaskReconciler>();
        services.TryAddScoped<IUserTaskInvitationService, DefaultUserTaskInvitationService>();
        services.TryAddScoped<UserTaskGuestActorResolver>();
        services.AddNotificationHandler<Handlers.UserTaskBookmarkPersistedHandler>();

        // Hosted workers own the periodic side of the module: due/timeout sweeps, projection reconciliation,
        // and draining the encrypted invitation outbox.
        services.AddHostedService<HostedServices.UserTaskDueWorker>();
        services.AddHostedService<HostedServices.UserTaskReconciliationWorker>();
        services.AddHostedService<HostedServices.UserTaskInvitationDeliveryWorker>();
        return services;
    }
}
