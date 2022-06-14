using System.Threading.Tasks;
using Elsa.Expressions.Models;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;

namespace Elsa.Samples.Web1.Activities;

public class MyGreeterComposite : Composite
{
    public MyGreeterComposite() : this(new Output<string>())
    {
    }

    public MyGreeterComposite(MemoryBlockReference name) : this(new Output<string>(name))
    {
    }

    public MyGreeterComposite(Output<string> name)
    {
        //var name = Name ?? new Output<string>();

        Root = new Sequence
        {
            Activities =
            {
                new WriteLine(context => context.Get(Prompt)),
                new ReadLine(name),
                new WriteLine(context => $"Nice to meet you, {context.Get(name)}!")
            }
        };
    }

    [Input] public Input<string> Prompt { get; set; } = new("Hello! What's your name?");
    [Output] public Output<string>? Name { get; set; } = new();

    protected override ValueTask OnCompletedAsync(ActivityExecutionContext context, ActivityExecutionContext childContext)
    {
        //context.Set(Name, _name.Get(childContext));
        return ValueTask.CompletedTask;
    }
}