using Elsa.Attributes;
using Elsa.Expressions.Models;
using Elsa.Implementations;
using Elsa.Models;
using Elsa.Services;

namespace Elsa.Activities;

[Activity("Elsa", "Console", "Read a line of text from the console.")]
public class ReadLine : Activity<string>
{
    public ReadLine()
    {
    }

    public ReadLine(RegisterLocationReference captureTarget) => this.CaptureOutput(captureTarget);

    protected override void Execute(ActivityExecutionContext context)
    {
        var provider = context.GetService<IStandardInStreamProvider>() ?? new StandardInStreamProvider(System.Console.In);
        var reader = provider.GetTextReader();
        var text = reader.ReadLine();
        context.Set(Result, text);
    }
}