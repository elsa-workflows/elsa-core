using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;

namespace Elsa.Workflows.Core.PortResolvers;

/// <summary>
/// Returns a list of outbound activities for a given <see cref="Switch"/> activity's branches.
/// </summary>
public class SwitchActivityPortResolver : IActivityPortResolver
{
    /// <inheritdoc />
    public int Priority => 0;

    /// <inheritdoc />
    public bool GetSupportsActivity(IActivity activity) => activity is Switch;

    /// <inheritdoc />
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