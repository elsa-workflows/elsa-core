using Elsa.Activities.Console;
using Elsa.Builders;

namespace Elsa.Samples.BuildAndDispatchConsole
{
    /// <summary>
    /// This workflow prompts the user to enter an integer start value, then iterates back from that value to 0.
    /// The workflow also demonstrates retrieving runtime values such as user input. 
    /// </summary>
    public class DemoWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder.WriteLine("I am demo.");
        }
    }
}