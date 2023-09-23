using Elsa.DropIns.Core;
using Elsa.Extensions;
using Elsa.Workflows.Core.Contracts;
using SampleDropIn.Activities;

namespace SampleDropIn;

public class DropInStartup : IDropInStartup
{
    private readonly IActivityRegistry _activityRegistry;

    public DropInStartup(IActivityRegistry activityRegistry)
    {
        _activityRegistry = activityRegistry;
    }
    
    public async ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        await _activityRegistry.RegisterAsync<SampleActivity>(cancellationToken: cancellationToken);
        await _activityRegistry.RegisterAsync<SampleActivity>(cancellationToken: cancellationToken);
    }
}