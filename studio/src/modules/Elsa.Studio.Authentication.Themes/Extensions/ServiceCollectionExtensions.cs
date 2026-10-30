using Elsa.Studio.Authentication.Themes.Components;
using Elsa.Studio.Authentication.UI.Extensions;
using Elsa.Studio.Authentication.UI.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Authentication.Themes.Extensions;

/// <summary>
/// Registers the optional first-party modern login-theme collection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the first-party presentation themes. Call after <c>AddAuthenticationUI</c>.
    /// </summary>
    public static IServiceCollection AddElsaStudioLoginThemes(this IServiceCollection services)
    {
        return services
            .AddLoginTheme<WorkflowConstellationLoginTheme>(LoginThemeIds.WorkflowConstellation)
            .AddLoginTheme<WorkflowAuroraLoginTheme>(LoginThemeIds.WorkflowAurora)
            .AddLoginTheme<ExecutionTimelineLoginTheme>(LoginThemeIds.ExecutionTimeline)
            .AddLoginTheme<HumanAutomationLoginTheme>(LoginThemeIds.HumanAutomation);
    }
}
