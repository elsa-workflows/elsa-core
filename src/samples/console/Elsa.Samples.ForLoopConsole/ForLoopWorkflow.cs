using System;
using Elsa.Activities.Console;
using Elsa.Activities.ControlFlow;
using Elsa.Activities.Primitives;
using Elsa.Builders;

namespace Elsa.Samples.ForLoopConsole
{
    /// <summary>
    /// This workflow prompts the user to enter an integer start value, then iterates back from that value to 0.
    /// The workflow also demonstrates retrieving runtime values such as user input. 
    /// </summary>
    public class ForLoopWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .WriteLine("Enter a number:")
                .ReadLine()
                .SetVariable("From", context => int.Parse(((string)context.Input)!))
                .WriteLine(context => $"Counting down from {Math.Abs(context.GetVariable<int>("From")!)} to 0...")
                .For(
                    context => context.GetVariable<int>("From"),
                    _ => 0,
                    _ => -1,
                    iterate => iterate.WriteLine(context => $"Iteration {context.Input}"),
                    Operator.GreaterThanOrEqual)
                .WriteLine("Done.");
        }
    }
}