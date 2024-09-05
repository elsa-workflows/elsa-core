using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Models;

namespace Elsa.Server.Web.Activities;

public class Activity2 : CodeActivity
{
    public Input<string> MyInput { get; set; } = default!;

    protected override void Execute(ActivityExecutionContext context)
    {
        var input = MyInput.Get(context);
        Console.WriteLine($"Aha! Input received: {input}");
    }
}