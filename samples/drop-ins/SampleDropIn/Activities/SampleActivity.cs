using Elsa.Workflows;

namespace SampleDropIn.Activities;

public class SampleActivity : CodeActivity
{
    protected override void Execute(ActivityExecutionContext context)
    {
        Console.WriteLine("Hello, world! From SampleDropIn!");
    }
}