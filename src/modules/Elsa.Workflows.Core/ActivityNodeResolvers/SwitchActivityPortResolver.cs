using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.ActivityNodeResolvers;

/// <summary>
/// Returns a list of outbound activities for a given <see cref="Switch"/> activity's branches.
/// </summary>
public class SwitchActivityPortResolver : IActivityPortResolver
{
    public int Priority => 0;
    public bool GetSupportsActivity(IActivity activity) => activity is Switch;

    public ValueTask<IEnumerable<IActivity>> GetPortsAsync(IActivity activity, CancellationToken cancellationToken = default)
    {
        var ports = GetPortsInternal(activity);
        return new(ports);
    }

    private IEnumerable<IActivity> GetPortsInternal(IActivity activity)
    {
        var @switch = (Switch)activity;
        var cases = @switch.Cases.Where(x => x.Activity != null);

        foreach (var @case in cases)
            yield return @case.Activity!;

        if (@switch.Default != null)
            yield return @switch.Default;
    }
}