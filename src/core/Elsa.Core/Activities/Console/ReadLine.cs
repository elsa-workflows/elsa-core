using Elsa.Attributes;
using Elsa.Models;

namespace Elsa.Activities.Console;

public class ReadLine : Activity
{
    public ReadLine()
    {
    }

    public ReadLine(Variable variable, Func<object?, object?>? valueConverter = default) => Output = new Output<string?>(variable, valueConverter);

    [Output] public Output<string?>? Output { get; set; }

    protected override void Execute(ActivityExecutionContext context)
    {
        var text = System.Console.ReadLine();
        context.Set(Output, text);
    }
}