using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.PortResolvers;

/// <summary>
/// Returns a list of outbound activities for a given <see cref="Switch"/> activity's branches.
/// </summary>
public class SwitchActivityResolver : IActivityResolver
{
    /// <inheritdoc />
    public int Priority => 0;

    /// <inheritdoc />
    public bool GetSupportsActivity(IActivity activity) => activity is Switch;

    /// <inheritdoc />
    public ValueTask<IEnumerable<ActivityPort>> GetActivityPortsAsync(IActivity activity, CancellationToken cancellationToken = default)
    {
        var ports = GetPortsInternal(activity).ToList();
        return new(ports);
    }

    private static IEnumerable<ActivityPort> GetPortsInternal(IActivity activity)
    {
        var @switch = (Switch)activity;
        var cases = @switch.Cases.Where(x => x.Activity != null);

        foreach (var @case in cases)
            yield return ActivityPort.FromActivity(@case.Activity!, @case.Label);

        if (@switch.Default != null)
            yield return ActivityPort.FromActivity(@switch.Default, nameof(Switch.Default));
    }
}