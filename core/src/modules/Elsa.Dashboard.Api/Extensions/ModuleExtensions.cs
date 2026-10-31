using Elsa.Dashboard.Api.Features;
using Elsa.Features.Services;

namespace Elsa.Extensions;

public static class DashboardModuleExtensions
{
    public static IModule UseDashboardApi(this IModule module, Action<DashboardApiFeature>? configure = null)
    {
        module.Configure(configure);
        return module;
    }
}
