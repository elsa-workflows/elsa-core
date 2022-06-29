using Elsa.Features.Services;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Management.Features;

namespace Elsa.Workflows.Management.Extensions;

public static class DependencyInjectionExtensions
{
    public static IModule UseManagement(this IModule module, Action<WorkflowManagementFeature>? configure = default)
    {
        module.Configure<WorkflowManagementFeature>(management =>
        {
            management.AddActivity<NotFoundActivity>();
            configure?.Invoke(management);
        });
        return module;
    }
}