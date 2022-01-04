using System.Threading.Tasks;
using Elsa.Activities.Console;
using Elsa.Activities.Workflows;
using Elsa.Attributes;
using Elsa.Models;

namespace Elsa.Samples.Web1.Activities;

public class MyGreeterComposite : Composite
{
    private readonly Variable _name = new Variable<string>();

    public MyGreeterComposite()
    {
        Root = new Sequence
        {
            Variables = { _name },
            Activities =
            {
                new WriteLine(context => context.Get(Prompt)),
                new ReadLine(_name),
                new WriteLine(context => $"Nice to meet you, {_name.Get(context)}!")
            }
        };
    }

    [Input] public Input<string> Prompt { get; set; } = new("Hello! What's your name?");
    [Output] public Output<string?>? Name { get; set; }

    protected override ValueTask OnCompletedAsync(ActivityExecutionContext context, ActivityExecutionContext childContext)
    {
        context.Set(Name, _name.Get(childContext));
        return ValueTask.CompletedTask;
    }
}