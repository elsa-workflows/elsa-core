using Elsa.AspNetCore.Parts;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.AspNetCore.Extensions;

public static class MvcBuilderExtensions
{
    /// <summary>
    /// Remove all application parts. This allows for manually controlling what controllers to add.
    /// </summary>
    public static IMvcBuilder ClearApplicationParts(this IMvcBuilder mvcBuilder) =>
        mvcBuilder.ConfigureApplicationPartManager(partManager => 
            partManager.ApplicationParts.Clear());

    /// <summary>
    /// Add application parts from the assembly containing the specified type.
    /// </summary>
    public static IMvcBuilder AddApplicationPartsFrom<T>(this IMvcBuilder mvcBuilder) => 
        mvcBuilder.ConfigureApplicationPartManager(partManager => 
            partManager.ApplicationParts.Add(new AssemblyPart(typeof(T).Assembly)));

    /// <summary>
    /// Add the specified controller types.
    /// </summary>
    public static IMvcBuilder AddControllerTypes(this IMvcBuilder mvcBuilder, params Type[] controllerTypes) => 
        mvcBuilder.ConfigureApplicationPartManager(partManager => 
            partManager.ApplicationParts.Add(new TypesPart(controllerTypes)));
}