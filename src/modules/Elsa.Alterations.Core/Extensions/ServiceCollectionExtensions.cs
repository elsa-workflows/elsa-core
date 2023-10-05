using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Core.Options;
using Elsa.Alterations.Core.Serialization;
using Elsa.Alterations.Core.Services;
using Elsa.Common.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Alterations.Core.Extensions;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the core Elsa alterations services.
    /// </summary>
    public static IServiceCollection AddAlterationsCore(this IServiceCollection services)
    {
        services.Configure<AlterationOptions>(_ => { }); // Ensure that the options are configured even if the application doesn't do so.
        services.AddSingleton<IAlterationPlanScheduler, DefaultAlterationPlanScheduler>();
        services.AddSingleton<IAlterationJobRunner, DefaultAlterationJobRunner>();
        services.AddSingleton<IAlterationRunner, DefaultAlterationRunner>();
        services.AddSingleton<IAlteredWorkflowDispatcher, DefaultAlteredWorkflowDispatcher>();
        services.AddSingleton<IAlterationSerializer, AlterationSerializer>();
        services.AddSingleton<ISerializationOptionsConfigurator, AlterationSerializationOptionConfigurator>();
        return services;
    }

    /// <summary>
    /// Adds an alteration handler.
    /// </summary>
    public static IServiceCollection AddAlteration<T, THandler>(this IServiceCollection services) where T : IAlteration where THandler : class, IAlterationHandler
    {
        services.Configure<AlterationOptions>(options => options.AlterationTypes.Add(typeof(T)));
        services.AddSingleton<IAlterationHandler, THandler>();
        return services;
    }
}