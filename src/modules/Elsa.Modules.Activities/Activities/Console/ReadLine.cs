using Elsa.Attributes;
using Elsa.Models;
using Elsa.Modules.Activities.Contracts;

namespace Elsa.Modules.Activities.Activities.Console;

[Activity("Elsa", "Console", "Read a line of text from the console.")]
public class ReadLine : Activity<string>
{
    public ReadLine()
    {
    }

    public ReadLine(RegisterLocationReference captureTarget) => this.CaptureOutput(captureTarget);

    protected override void Execute(ActivityExecutionContext context)
    {
        var provider = context.GetRequiredService<IStandardInStreamProvider>();
        var reader = provider.GetTextReader();
        var text = reader.ReadLine();
        context.Set(Result, text);
    }
}