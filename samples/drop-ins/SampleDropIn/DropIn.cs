using Elsa.DropIns.Core;
using Elsa.Extensions;
using Elsa.Features.Services;
using Elsa.Workflows;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SampleDropIn.Activities;

namespace SampleDropIn;

[PublicAPI]
public class DropIn : IDropIn
{
    public void Install(IModule module)
    {
        module.AddActivitiesFrom<DropIn>();
    }

    public async ValueTask ConfigureAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        var activityRegistry = serviceProvider.GetRequiredService<IActivityRegistry>();
        await activityRegistry.RegisterAsync<SampleActivity>(cancellationToken: cancellationToken);
    }

    public void Unconfigure(IServiceProvider serviceProvider)
    {
        var activityRegistry = serviceProvider.GetRequiredService<IActivityRegistry>();
        activityRegistry.Remove(typeof(ActivityRegistry), activityRegistry.Find<SampleActivity>()!);
    }
}