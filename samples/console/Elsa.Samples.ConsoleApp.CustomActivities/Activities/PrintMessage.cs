using Elsa.Workflows;

public class PrintMessage : CodeActivity
{
    public string Message { get; set; }
    
    protected override void Execute(ActivityExecutionContext context)
    {
        Console.WriteLine(Message);
    }
}