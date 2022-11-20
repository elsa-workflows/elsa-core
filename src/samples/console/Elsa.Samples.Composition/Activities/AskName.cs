using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Models;

public class AskName : Composite<string>
{
    private readonly Variable<string> _name = new();

    public Input<string> Prompt { get; set; } = new("Please tell me your name:");

    public AskName()
    {
        Root = new Sequence
        {
            Variables = { _name },
            Activities =
            {
                new WriteLine(context => Prompt.Get(context)),
                new ReadLine(_name)
            }
        };
    }

    protected override void OnCompleted(ActivityExecutionContext context, ActivityExecutionContext childContext)
    {
        var name = _name.Get<string>(context);
        context.Set(Result, name);
    }
}