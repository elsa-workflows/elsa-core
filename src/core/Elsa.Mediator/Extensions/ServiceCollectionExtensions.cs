using System.Reflection;
using Elsa.Mediator.Contracts;
using Elsa.Mediator.Middleware.Command;
using Elsa.Mediator.Middleware.Command.Contracts;
using Elsa.Mediator.Middleware.Notification;
using Elsa.Mediator.Middleware.Notification.Contracts;
using Elsa.Mediator.Middleware.Request;
using Elsa.Mediator.Middleware.Request.Contracts;
using Elsa.Mediator.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Mediator.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMediator(this IServiceCollection services)
    {
        return services
            .AddSingleton<IMediator, DefaultMediator>()
            .AddSingleton<IRequestSender>(sp => sp.GetRequiredService<IMediator>())
            .AddSingleton<ICommandSender>(sp => sp.GetRequiredService<IMediator>())
            .AddSingleton<IPublisher>(sp => sp.GetRequiredService<IMediator>())
            .AddSingleton<IRequestPipeline, RequestPipeline>()
            .AddSingleton<ICommandPipeline, CommandPipeline>()
            .AddSingleton<INotificationPipeline, NotificationPipeline>();
    }

    public static IServiceCollection AddCommandHandler<THandler, TCommand>(this IServiceCollection services)
        where THandler : class, ICommandHandler<TCommand>
        where TCommand : ICommand<Unit> =>
        services.AddCommandHandler<THandler, TCommand, Unit>();

    public static IServiceCollection AddCommandHandler<THandler, TCommand, TResult>(this IServiceCollection services)
        where THandler : class, ICommandHandler<TCommand, TResult>
        where TCommand : ICommand<TResult>
    {
        return services.AddSingleton<ICommandHandler, THandler>();
    }

    public static IServiceCollection AddRequestHandler<THandler, TRequest, TResponse>(this IServiceCollection services)
        where THandler : class, IRequestHandler<TRequest, TResponse>
        where TRequest : IRequest<TResponse>?
    {
        return services.AddSingleton<IRequestHandler, THandler>();
    }

    public static IServiceCollection AddHandlersFrom<TMarker>(this IServiceCollection services) => services.AddHandlersFrom(typeof(TMarker));
    public static IServiceCollection AddHandlersFrom(this IServiceCollection services, Type markerType) => services.AddHandlersFrom(markerType.Assembly);

    public static IServiceCollection AddHandlersFrom(this IServiceCollection services, Assembly assembly)
    {
        return services
            .AddNotificationHandlersFrom(assembly)
            .AddRequestHandlersFrom(assembly)
            .AddCommandHandlersFrom(assembly);
    }

    public static IServiceCollection AddNotificationHandlersFrom<TMarker>(this IServiceCollection services) => services.AddHandlersFromInternal<INotificationHandler>(typeof(TMarker));
    public static IServiceCollection AddNotificationHandlersFrom(this IServiceCollection services, Type markerType) => services.AddHandlersFromInternal<INotificationHandler>(markerType);
    public static IServiceCollection AddNotificationHandlersFrom(this IServiceCollection services, Assembly assembly) => services.AddHandlersFromInternal<INotificationHandler>(assembly);

    public static IServiceCollection AddRequestHandlersFrom<TMarker>(this IServiceCollection services) => services.AddHandlersFromInternal<IRequestHandler>(typeof(TMarker));
    public static IServiceCollection AddRequestHandlersFrom(this IServiceCollection services, Type markerType) => services.AddHandlersFromInternal<IRequestHandler>(markerType);
    public static IServiceCollection AddRequestHandlersFrom(this IServiceCollection services, Assembly assembly) => services.AddHandlersFromInternal<IRequestHandler>(assembly);

    public static IServiceCollection AddCommandHandlersFrom<TMarker>(this IServiceCollection services) => services.AddHandlersFromInternal<ICommandHandler>(typeof(TMarker));
    public static IServiceCollection AddCommandHandlersFrom(this IServiceCollection services, Type markerType) => services.AddHandlersFromInternal<ICommandHandler>(markerType);
    public static IServiceCollection AddCommandHandlersFrom(this IServiceCollection services, Assembly assembly) => services.AddHandlersFromInternal<ICommandHandler>(assembly);


    private static IServiceCollection AddHandlersFromInternal<TService, TMarker>(this IServiceCollection services) => services.AddHandlersFromInternal<TService>(typeof(TMarker));
    private static IServiceCollection AddHandlersFromInternal<TService>(this IServiceCollection services, Type assemblyMarkerType) => services.AddHandlersFromInternal<TService>(assemblyMarkerType.Assembly);

    private static IServiceCollection AddHandlersFromInternal<TService>(this IServiceCollection services, Assembly assembly)
    {
        var serviceType = typeof(TService);
        var types = assembly.ExportedTypes.Where(x => serviceType.IsAssignableFrom(x));

        foreach (var type in types)
            services.AddSingleton(serviceType, type);

        return services;
    }
}