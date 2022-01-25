using Elsa.Contracts;
using Elsa.Models;

namespace Elsa.Activities.ControlFlow;

public class Switch : Activity
{
    public ICollection<SwitchCase> Cases { get; set; } = new List<SwitchCase>();
    public IActivity? Default { get; set; }

    protected override void Execute(ActivityExecutionContext context)
    {
        var firstMatch = Cases.FirstOrDefault(x => context.Get(x.Condition));

        if (firstMatch == null)
        {
            if (Default != null)
                context.ScheduleActivity(Default);

            return;
        }

        if (firstMatch.Activity != null)
            context.ScheduleActivity(firstMatch.Activity);
    }
}

public class SwitchCase
{
    public SwitchCase()
    {
    }

    public string Label { get; set; } = default!;
    public Input<bool> Condition { get; set; } = new(false);
    public IActivity? Activity { get; set; }
}