using Elsa.Alterations.Core.Contracts;
using Elsa.Alterations.Core.Options;
using Elsa.Alterations.Core.Serialization;
using Elsa.Alterations.Core.Services;
using Elsa.Extensions;
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
        services.AddScoped<IAlteredWorkflowDispatcher, AlteredWorkflowDispatcher>();
        services.AddScoped<IWorkflowInstanceFinder, WorkflowInstanceFinder>();
        services.AddSingleton<IAlterationSerializer, AlterationSerializer>();
        services.AddSerializationOptionsConfigurator<AlterationSerializationOptionConfigurator>();
        return services;
    }

    /// <summary>
    /// Adds an alteration handler.
    /// </summary>
    public static IServiceCollection AddAlteration<T, THandler>(this IServiceCollection services) where T : IAlteration where THandler : class, IAlterationHandler
    {
        services.Configure<AlterationOptions>(options => options.AlterationTypes.Add(typeof(T)));
        services.AddScoped<IAlterationHandler, THandler>();
        return services;
    }
}