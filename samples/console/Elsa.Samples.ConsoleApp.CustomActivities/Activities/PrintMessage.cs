using Elsa.Workflows;

public class PrintMessage : CodeActivity
{
    public void Execute(ActivityExecutionContext context)
    {
        Console.WriteLine("Hello world!");
    }
}