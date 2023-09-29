using Elsa.Alterations.AlterationTypes;
using Elsa.Alterations.Core.Extensions;
using Elsa.Alterations.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Alterations.Extensions;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds various alteration handlers.
    /// </summary>
    public static IServiceCollection AddAlterations(this IServiceCollection services)
    {
        services.AddAlterationHandler<MigrateHandler>();
        services.AddAlterationHandler<ModifyVariableHandler>();
        services.AddAlterationHandler<ScheduleFlowchartActivityHandler>();
        return services;
    }
}