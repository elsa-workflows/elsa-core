using Elsa.Workflows.Api.Endpoints.ActivityDescriptors;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Api.Extensions;

public static class MvcBuilderExtensions
{
    /// <summary>
    /// Add all API controllers from this assembly.
    /// </summary>
    public static IMvcBuilder AddWorkflowManagementApiControllers(this IMvcBuilder mvcBuilder)
    {
        return mvcBuilder.ConfigureApplicationPartManager(partManager =>
        {
            // Add controllers from Elsa.Workflows.Api:
            partManager.ApplicationParts.Add(new AssemblyPart(typeof(List).Assembly));
        });
    }
}