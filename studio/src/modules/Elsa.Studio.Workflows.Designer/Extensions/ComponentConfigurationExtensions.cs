using Elsa.Studio.Workflows.Designer.Components.ActivityWrappers.V2;
using Microsoft.AspNetCore.Components.Web;

namespace Elsa.Studio.Workflows.Designer.Extensions;

/// <summary>
/// Contains extension methods for <see cref="IJSComponentConfiguration"/>.
/// </summary>
public static class ComponentConfigurationExtensions
{
    /// <summary>
    /// Registers custom elements.
    /// </summary>
    public static IJSComponentConfiguration RegisterCustomElsaStudioElements(this IJSComponentConfiguration configuration, Type? activityComponentType = null)
    {
        activityComponentType ??= typeof(ActivityWrapper);
        configuration.RegisterForJavaScript(activityComponentType, "elsa-activity-wrapper", "registerBlazorCustomElement");

        return configuration;
    }
}