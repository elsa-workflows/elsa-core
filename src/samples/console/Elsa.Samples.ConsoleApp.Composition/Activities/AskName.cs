using Elsa.Extensions;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Memory;
using Elsa.Workflows.Core.Models;

namespace Elsa.Samples.ConsoleApp.Composition.Activities;

public class AskName : Composite<string>
{
    private readonly Variable<string> _name = new();

    public Input<string> Prompt { get; set; } = new("Please tell me your name:");

    public AskName()
    {
        Variables = new List<Variable> { _name };
        Root = new Sequence
        {
            Activities = new List<IActivity>
            {
                new WriteLine(context => Prompt.Get(context)),
                new ReadLine(_name)
            }
        };
    }

    protected override void OnCompleted(ActivityCompletedContext context)
    {
        var name = _name.Get<string>(context.TargetContext);
        context.TargetContext.Set(Result, name);
    }
}