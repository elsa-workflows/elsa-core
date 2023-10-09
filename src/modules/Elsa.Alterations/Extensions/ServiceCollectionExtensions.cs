using Elsa.Alterations.AlterationHandlers;
using Elsa.Alterations.AlterationTypes;
using Elsa.Alterations.Core.Extensions;
using Elsa.Alterations.Features;
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
        services.AddAlteration<Migrate, MigrateHandler>();
        services.AddAlteration<ModifyVariable, ModifyVariableHandler>();
        services.AddAlteration<ScheduleActivity, ScheduleActivityHandler>();
        services.AddAlteration<CancelActivity, CancelActivityHandler>();
        services.AddNotificationHandlersFrom<AlterationsFeature>();
        return services;
    }
}