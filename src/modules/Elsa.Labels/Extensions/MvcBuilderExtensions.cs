using Elsa.Labels.Endpoints.Labels;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Labels.Extensions;

public static class MvcBuilderExtensions
{
    /// <summary>
    /// Add all API controllers from this assembly.
    /// </summary>
    public static IMvcBuilder AddLabelsApiControllers(this IMvcBuilder mvcBuilder) => 
        mvcBuilder.ConfigureApplicationPartManager(partManager => 
            partManager.ApplicationParts.Add(new AssemblyPart(typeof(List).Assembly)));
}