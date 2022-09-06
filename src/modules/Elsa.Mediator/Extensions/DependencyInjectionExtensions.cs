using System.Reflection;
using System.Threading.Channels;
using Elsa.Mediator.Implementations;
using Elsa.Mediator.Middleware.Command;
using Elsa.Mediator.Middleware.Command.Contracts;
using Elsa.Mediator.Middleware.Notification;
using Elsa.Mediator.Middleware.Notification.Contracts;
using Elsa.Mediator.Middleware.Request;
using Elsa.Mediator.Middleware.Request.Contracts;
using Elsa.Mediator.Models;
using Elsa.Mediator.Services;
using Elsa.Features.Services;
using Elsa.Mediator.Features;
using Elsa.Mediator.HostedServices;
using Elsa.Mediator.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.Mediator.Extensions;

public static class DependencyInjectionExtensions
{
    public static IModule AddMediator(this IModule module, Action<MediatorFeature>? configure = default)
    {
        module.Configure(configure);
        return module;
    }
    
    public static IServiceCollection AddMediator(this IServiceCollection services, Action<MediatorOptions>? configure = default)
    { 
        services.Configure(configure ?? (_ => { }));
        
        return services
            .AddSingleton<IMediator, DefaultMediator>()
            .AddSingleton<IRequestSender>(sp => sp.GetRequiredService<IMediator>())
            .AddSingleton<ICommandSender>(sp => sp.GetRequiredService<IMediator>())
            .AddSingleton<IEventPublisher>(sp => sp.GetRequiredService<IMediator>())
            .AddSingleton<IBackgroundCommandSender, BackgroundCommandSender>()
            .AddSingleton<IBackgroundEventPublisher, BackgroundEventPublisher>()
            .AddSingleton<IRequestPipeline, RequestPipeline>()
            .AddSingleton<ICommandPipeline, CommandPipeline>()
            .AddSingleton<INotificationPipeline, NotificationPipeline>()
            
            .AddHostedService(sp =>
            {
                var options = sp.GetRequiredService<IOptions<MediatorOptions>>().Value;
                return ActivatorUtilities.CreateInstance<BackgroundCommandSenderHostedService>(sp, options.CommandWorkerCount);
            })
            
            .AddHostedService(sp =>
            {
                var options = sp.GetRequiredService<IOptions<MediatorOptions>>().Value;
                return ActivatorUtilities.CreateInstance<BackgroundEventPublisherHostedService>(sp, options.NotificationWorkerCount);
            })
            
            .CreateChannel<ICommand>()
            .CreateChannel<INotification>()
            ;
    }

    public static IServiceCollection AddConsumer<T, TConsumer>(this IServiceCollection services, int workers = 1) where TConsumer : class, IConsumer<T>
    {
        services.AddSingleton<IConsumer<T>, TConsumer>();

        services.AddHostedService(sp =>
        {
            var channel = Channel.CreateUnbounded<T>();
            var consumer = sp.GetRequiredService<IConsumer<T>>();
            var logger = sp.GetRequiredService<ILogger<MessageProcessorHostedService<T>>>();
            return new MessageProcessorHostedService<T>(workers, channel, consumer, logger);
        });
        return services;
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

    public static IServiceCollection CreateChannel<T>(this IServiceCollection services) =>
        services
            .AddSingleton(CreateChannel<T>())
            .AddTransient(CreateChannelReader<T>)
            .AddTransient(CreateChannelWriter<T>);
    
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
    
    private static Channel<T> CreateChannel<T>() => Channel.CreateUnbounded<T>(new UnboundedChannelOptions());
    private static ChannelReader<T> CreateChannelReader<T>(IServiceProvider serviceProvider) => serviceProvider.GetRequiredService<Channel<T>>().Reader;
    private static ChannelWriter<T> CreateChannelWriter<T>(IServiceProvider serviceProvider) => serviceProvider.GetRequiredService<Channel<T>>().Writer;
}