using Elsa.Activities.ControlFlow;
using Elsa.Contracts;

namespace Elsa.ActivityNodeResolvers;

public class SwitchActivityNodeResolver : IActivityNodeResolver
{
    public int Priority => 0;
    public bool GetSupportsActivity(IActivity activity) => activity is Switch;

    public IEnumerable<IActivity> GetPorts(IActivity activity)
    {
        var @switch = (Switch)activity;
        var cases = @switch.Cases.Where(x => x.Activity != null);

        foreach (var @case in cases)
            yield return @case.Activity!;

        if (@switch.Default != null)
            yield return @switch.Default;
    }
}