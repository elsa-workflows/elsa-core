using System.Reflection;
using System.Threading.Channels;
using Elsa.Mediator.Channels;
using Elsa.Mediator.Contracts;
using Elsa.Mediator.HostedServices;
using Elsa.Mediator.Middleware.Command;
using Elsa.Mediator.Middleware.Command.Contracts;
using Elsa.Mediator.Middleware.Notification;
using Elsa.Mediator.Middleware.Notification.Contracts;
using Elsa.Mediator.Middleware.Request;
using Elsa.Mediator.Middleware.Request.Contracts;
using Elsa.Mediator.Models;
using Elsa.Mediator.Options;
using Elsa.Mediator.Services;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Adds mediator services to the <see cref="IServiceCollection"/>.
/// </summary>
[PublicAPI]
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Adds mediator services to the <see cref="IServiceCollection"/>.
    /// </summary>
    public static IServiceCollection AddMediator(this IServiceCollection services, Action<MediatorOptions>? configure = default)
    {
        services.Configure(configure ?? (_ => { }));

        return services
                .AddScoped<IMediator, DefaultMediator>()
                .AddScoped<IRequestSender>(sp => sp.GetRequiredService<IMediator>())
                .AddScoped<ICommandSender>(sp => sp.GetRequiredService<IMediator>())
                .AddScoped<INotificationSender>(sp => sp.GetRequiredService<IMediator>())
                .AddScoped<IRequestPipeline, RequestPipeline>()
                .AddScoped<ICommandPipeline, CommandPipeline>()
                .AddScoped<INotificationPipeline, NotificationPipeline>()
            ;
    }

    /// <summary>
    /// Adds mediator hosted services to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddMediatorHostedServices(this IServiceCollection services)
    {
        return services
            .AddSingleton<INotificationsChannel, NotificationsChannel>()
            .AddSingleton<ICommandsChannel, CommandsChannel>()
            .AddSingleton<IJobsChannel, JobsChannel>()
            .AddSingleton<IJobQueue, JobQueue>()
            .AddHostedService<JobRunnerHostedService>()
            .AddHostedService(sp =>
            {
                using var scope = sp.CreateScope();

                var options = scope.ServiceProvider.GetRequiredService<IOptions<MediatorOptions>>().Value;
                return ActivatorUtilities.CreateInstance<BackgroundCommandSenderHostedService>(scope.ServiceProvider, options.CommandWorkerCount);
            })
            .AddHostedService(sp =>
            {
                using var scope = sp.CreateScope();

                var options = scope.ServiceProvider.GetRequiredService<IOptions<MediatorOptions>>().Value;
                return ActivatorUtilities.CreateInstance<BackgroundEventPublisherHostedService>(scope.ServiceProvider, options.NotificationWorkerCount);
            });
    }

    /// <summary>
    /// Adds a <see cref="Channel{T}"/> to the <see cref="IServiceCollection"/> and a hosted service that continuously reads from the channel and executes each received message.
    /// </summary>
    public static IServiceCollection AddMessageChannel<T>(this IServiceCollection services, int workerCount = 1) where T : notnull
    {
        services.AddSingleton<Channel<T>>(_ => CreateChannel<T>());

        services.AddHostedService(sp =>
        {
            var channel = sp.GetRequiredService<Channel<T>>();
            var consumers = sp.GetServices<IConsumer<T>>();
            var logger = sp.GetRequiredService<ILogger<MessageProcessorHostedService<T>>>();
            return new MessageProcessorHostedService<T>(workerCount, channel, consumers, logger);
        });
        return services;
    }

    /// <summary>
    /// Adds a channel consumer.
    /// </summary>
    public static IServiceCollection AddMessageConsumer<T, TConsumer>(this IServiceCollection services) where TConsumer : class, IConsumer<T> where T : notnull
    {
        return services.AddScoped<IConsumer<T>, TConsumer>();
    }

    /// <summary>
    /// Registers a <see cref="ICommandHandler{T}"/> with the service container.
    /// </summary>
    public static IServiceCollection AddCommandHandler<THandler>(this IServiceCollection services) where THandler : class, ICommandHandler =>
        services.AddScoped<ICommandHandler, THandler>();

    /// <summary>
    /// Registers a <see cref="ICommandHandler{T}"/> with the service container.
    /// </summary>
    public static IServiceCollection AddCommandHandler<THandler, TCommand>(this IServiceCollection services)
        where THandler : class, ICommandHandler<TCommand> where TCommand : ICommand<Unit> =>
        services.AddCommandHandler<THandler, TCommand, Unit>();

    /// <summary>
    /// Registers a <see cref="ICommandHandler{T}"/> with the service container.
    /// </summary>
    public static IServiceCollection AddCommandHandler<THandler, TCommand, TResult>(this IServiceCollection services)
        where THandler : class, ICommandHandler<TCommand, TResult>
        where TCommand : ICommand<TResult>
    {
        return services.AddScoped<ICommandHandler, THandler>();
    }

    /// <summary>
    /// Registers a <see cref="ICommandHandler{T}"/> with the service container.
    /// </summary>
    public static IServiceCollection AddNotificationHandler<THandler>(this IServiceCollection services)
        where THandler : class, INotificationHandler
    {
        return services.AddScoped<INotificationHandler, THandler>();
    }

    /// <summary>
    /// Registers a <see cref="INotificationHandler{T}"/> with the service container.
    /// </summary>
    public static IServiceCollection AddNotificationHandler<THandler, TNotification>(this IServiceCollection services)
        where THandler : class, INotificationHandler<TNotification>
        where TNotification : INotification =>
        services.AddScoped<INotificationHandler, THandler>();

    /// <summary>
    /// Registers a <see cref="INotificationHandler{T}"/> with the service container.
    /// </summary>
    public static IServiceCollection AddNotificationHandler<THandler, TNotification>(this IServiceCollection services, Func<IServiceProvider, THandler> factory)
        where THandler : class, INotificationHandler<TNotification>
        where TNotification : INotification =>
        services.AddScoped<INotificationHandler, THandler>(factory);

    /// <summary>
    /// Registers a <see cref="IRequestHandler{TRequest,TResponse}"/> with the service container.
    /// </summary>
    public static IServiceCollection AddRequestHandler<THandler, TRequest, TResponse>(this IServiceCollection services)
        where THandler : class, IRequestHandler<TRequest, TResponse>
        where TRequest : IRequest<TResponse>?
    {
        return services.AddScoped<IRequestHandler, THandler>();
    }

    /// <summary>
    /// Registers all handlers from the assembly of the specified <c>TMarker</c> type with the service container.
    /// </summary>
    public static IServiceCollection AddHandlersFrom<TMarker>(this IServiceCollection services) => services.AddHandlersFrom(typeof(TMarker));

    /// <summary>
    /// Registers all handlers from the assembly of the specified marker type with the service container.
    /// </summary>
    public static IServiceCollection AddHandlersFrom(this IServiceCollection services, Type markerType) => services.AddHandlersFrom(markerType.Assembly);

    /// <summary>
    /// Registers all handlers from the specified assembly with the service container.
    /// </summary>
    public static IServiceCollection AddHandlersFrom(this IServiceCollection services, Assembly assembly)
    {
        return services
            .AddNotificationHandlersFrom(assembly)
            .AddRequestHandlersFrom(assembly)
            .AddCommandHandlersFrom(assembly);
    }

    /// <summary>
    /// Registers all handlers from the assembly of the specified <c>TMarker</c> type with the service container.
    /// </summary>
    public static IServiceCollection AddNotificationHandlersFrom<TMarker>(this IServiceCollection services) => services.AddHandlersFromInternal<INotificationHandler>(typeof(TMarker));

    /// <summary>
    /// Registers all handlers from the assembly of the specified marker type with the service container.
    /// </summary>
    public static IServiceCollection AddNotificationHandlersFrom(this IServiceCollection services, Type markerType) => services.AddHandlersFromInternal<INotificationHandler>(markerType);

    /// <summary>
    /// Registers all handlers from the specified assembly with the service container.
    /// </summary>
    public static IServiceCollection AddNotificationHandlersFrom(this IServiceCollection services, Assembly assembly) => services.AddHandlersFromInternal<INotificationHandler>(assembly);

    /// <summary>
    /// Registers all handlers from the assembly of the specified <c>TMarker</c> type with the service container.
    /// </summary>
    public static IServiceCollection AddRequestHandlersFrom<TMarker>(this IServiceCollection services) => services.AddHandlersFromInternal<IRequestHandler>(typeof(TMarker));

    /// <summary>
    /// Registers all handlers from the assembly of the specified marker type with the service container.
    /// </summary>
    public static IServiceCollection AddRequestHandlersFrom(this IServiceCollection services, Type markerType) => services.AddHandlersFromInternal<IRequestHandler>(markerType);

    /// <summary>
    /// Registers all handlers from the specified assembly with the service container.
    /// </summary>
    public static IServiceCollection AddRequestHandlersFrom(this IServiceCollection services, Assembly assembly) => services.AddHandlersFromInternal<IRequestHandler>(assembly);

    /// <summary>
    /// Registers all handlers from the assembly of the specified <c>TMarker</c> type with the service container.
    /// </summary>
    public static IServiceCollection AddCommandHandlersFrom<TMarker>(this IServiceCollection services) => services.AddHandlersFromInternal<ICommandHandler>(typeof(TMarker));

    /// <summary>
    /// Registers all handlers from the assembly of the specified marker type with the service container.
    /// </summary>
    public static IServiceCollection AddCommandHandlersFrom(this IServiceCollection services, Type markerType) => services.AddHandlersFromInternal<ICommandHandler>(markerType);

    /// <summary>
    /// Registers all handlers from the specified assembly with the service container.
    /// </summary>
    public static IServiceCollection AddCommandHandlersFrom(this IServiceCollection services, Assembly assembly) => services.AddHandlersFromInternal<ICommandHandler>(assembly);

    private static IServiceCollection AddHandlersFromInternal<TService, TMarker>(this IServiceCollection services) => services.AddHandlersFromInternal<TService>(typeof(TMarker));
    private static IServiceCollection AddHandlersFromInternal<TService>(this IServiceCollection services, Type assemblyMarkerType) => services.AddHandlersFromInternal<TService>(assemblyMarkerType.Assembly);

    private static IServiceCollection AddHandlersFromInternal<TService>(this IServiceCollection services, Assembly assembly)
    {
        var serviceType = typeof(TService);
        var types = assembly.DefinedTypes.Where(x => serviceType.IsAssignableFrom(x));

        foreach (var type in types)
            services.AddScoped(serviceType, type);

        return services;
    }

    private static Channel<T> CreateChannel<T>() => Channel.CreateUnbounded<T>(new UnboundedChannelOptions());
    private static ChannelReader<T> CreateChannelReader<T>(IServiceProvider serviceProvider) => serviceProvider.GetRequiredService<Channel<T>>().Reader;
    private static ChannelWriter<T> CreateChannelWriter<T>(IServiceProvider serviceProvider) => serviceProvider.GetRequiredService<Channel<T>>().Writer;
}