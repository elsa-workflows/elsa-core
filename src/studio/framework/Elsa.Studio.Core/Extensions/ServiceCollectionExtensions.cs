using System.Diagnostics.CodeAnalysis;
using Elsa.Api.Client.Extensions;
using Elsa.Studio.Contracts;
using Elsa.Studio.Localization;
using Elsa.Studio.Models;
using Elsa.Studio.Options;
using Elsa.Studio.Services;
using Elsa.Studio.Visualizers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Elsa.Studio.Extensions;

/// <summary>
/// Contains extension methods for the <see cref="IServiceCollection"/> interface.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the core services.
    /// </summary>
    public static IServiceCollection AddCoreInternal(
        this IServiceCollection services,
        Action<StudioThemeOptions>? configureTheme = null)
    {
        var themeOptions = services.AddOptions<StudioThemeOptions>();
        if (configureTheme is not null)
            themeOptions.Configure(configureTheme);
        themeOptions.ValidateOnStart();

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<StudioThemeOptions>, StudioThemeOptionsValidator>());
        services.TryAddScoped<IStudioThemeRegistry, StudioThemeRegistry>();

        if (services.All(x => x.ServiceType != typeof(StudioThemeRegistrationMarker)))
        {
            services.AddSingleton<StudioThemeRegistrationMarker>();
            services
                .AddStudioThemeProvider<ClassicThemeProvider>(StudioThemeIds.Classic)
                .AddStudioThemeProvider<WorkflowConstellationThemeProvider>(StudioThemeIds.WorkflowConstellation)
                .AddStudioThemeProvider<WorkflowAuroraThemeProvider>(StudioThemeIds.WorkflowAurora)
                .AddStudioThemeProvider<ExecutionTimelineThemeProvider>(StudioThemeIds.ExecutionTimeline)
                .AddStudioThemeProvider<HumanAutomationThemeProvider>(StudioThemeIds.HumanAutomation);
        }

        services
            .AddScoped<IBlazorServiceAccessor, BlazorServiceAccessor>()
            .AddScoped<IMenuService, DefaultMenuService>()
            .AddScoped<IMenuGroupProvider, DefaultMenuGroupProvider>()
            .AddScoped<IAppBarService, DefaultAppBarService>()
            .AddScoped<IFeatureService, DefaultFeatureService>()
            .AddScoped<IUIHintService, DefaultUIHintService>()
            .AddScoped<IUIFieldExtensionService, DefaultUIFieldExtensionService>()
            .AddScoped<IExpressionService, DefaultExpressionService>()
            .AddScoped<IStartupTaskRunner, DefaultStartupTaskRunner>()
            .AddScoped<IServerInformationProvider, EmptyServerInformationProvider>()
            .AddScoped<IClientInformationProvider, AssemblyClientInformationProvider>()
            .AddScoped<IWidgetRegistry, DefaultWidgetRegistry>()
            .AddScoped<IActivityTabRegistry, DefaultActivityTabRegistry>()
            .AddSingleton<IContentVisualizerProvider, DefaultContentVisualizerProvider>()
            .AddUserMessageService<DefaultUserMessageService>()
            ;

        services.TryAddScoped<IThemeProvider, DefaultThemeProvider>();
        services.TryAddScoped<IThemeService, DefaultThemeService>();

        // Content visualizers
        services.AddContentVisualizer<JsonContentVisualizer>();

        // Mediator.
        services.AddScoped<IMediator, DefaultMediator>();

        //Localization
        services.AddSingleton<ILocalizationProvider, DefaultLocalizationProvider>();
        services.AddSingleton<ILocalizer, DefaultLocalizer>();

        // Single-flight coordinator for preventing concurrent operations.
        services.TryAddScoped<ISingleFlightCoordinator, SingleFlightCoordinator>();

        return services;
    }

    /// <summary>
    /// Registers an Elsa Studio theme pack.
    /// </summary>
    public static IServiceCollection AddStudioThemeProvider<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        this IServiceCollection services,
        string id)
        where T : class, IThemeProvider
    {
        services.AddScoped<T>();
        services.AddSingleton(new StudioThemeRegistration(id, typeof(T)));
        return services;
    }
    
    /// <summary>
    /// Adds backend services to the service collection.
    /// </summary>
    public static IServiceCollection AddRemoteBackend(this IServiceCollection services, BackendApiConfig? config = null)
    {
        services.Configure(config?.ConfigureBackendOptions ?? (_ => { }));
        services.AddDefaultApiClients(config?.ConfigureHttpClientBuilder);
        services.TryAddScoped<IRemoteBackendAccessor, DefaultRemoteBackendAccessor>();
        services.TryAddScoped<IBackendApiClientProvider, DefaultBackendApiClientProvider>();
        services.TryAddScoped<IAnonymousBackendApiClientProvider, DefaultAnonymousBackendApiClientProvider>();
        return services;
    }
    
    /// <summary>
    /// Provides the add remote api.
    /// </summary>
    public static IServiceCollection AddRemoteApi<TApi>(this IServiceCollection services, BackendApiConfig? config = null) where TApi : class
    {
        services.Configure(config?.ConfigureBackendOptions ?? (_ => { }));
        services.AddApiClient<TApi>(config?.ConfigureHttpClientBuilder);
        return services;
    }

    /// <summary>
    /// Adds the specified <see cref="INotificationHandler"/>.
    /// </summary>
    public static IServiceCollection AddNotificationHandler<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this IServiceCollection services) where T: class, INotificationHandler
    {
        return services.AddScoped<INotificationHandler, T>();
    }
    
    /// <summary>
    /// Adds the specified <see cref="IUIHintHandler"/>.
    /// </summary>
    public static IServiceCollection AddUIHintHandler<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this IServiceCollection services) where T : class, IUIHintHandler
    {
        return services.AddScoped<IUIHintHandler, T>();
    }

    /// <summary>
    /// Adds the specified <see cref="IUIFieldExtensionHandler"/>.
    /// </summary>
    public static IServiceCollection AddUIFieldEnhancerHandler<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this IServiceCollection services) where T : class, IUIFieldExtensionHandler
    {
        return services.AddScoped<IUIFieldExtensionHandler, T>();
    }

    /// <summary>
    /// Adds the specified <see cref="IUserMessageService"/>.
    /// </summary>
    public static IServiceCollection AddUserMessageService<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this IServiceCollection services) where T : class, IUserMessageService
    {
        return services.AddScoped<IUserMessageService, T>();
    }

    /// <summary>
    /// Adds the specified <see cref="IContentVisualizer"/>.
    /// </summary>
    public static IServiceCollection AddContentVisualizer<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(this IServiceCollection services) where T : class, IContentVisualizer
    {
        return services.AddTransient<IContentVisualizer, T>();
    }
}

internal sealed class StudioThemeRegistrationMarker;
