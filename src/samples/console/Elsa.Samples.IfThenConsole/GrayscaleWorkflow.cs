using System;
using Elsa.Activities.Console;
using Elsa.Activities.ControlFlow;
using Elsa.Builders;

namespace Elsa.Samples.IfThenConsole
{
    public class GrayscaleWorkflow : IWorkflow
    {
        private readonly Random _random;

        public GrayscaleWorkflow()
        {
            _random = new Random();
        }

        public void Build(IWorkflowBuilder workflow)
        {
            workflow
                .WriteLine("--Grayscale Calculator--", "Start")
                .WriteLine("Enter a number between 0 and 100.")
                .ReadLine()
                .IfThen(conditions =>
                {
                    var input = conditions.Context.GetInput<int>();
                    conditions
                        .Add("Black", input >= 0 && input < 20)
                        .Add("Dark Gray", input >= 20 && input < 50)
                        .Add("Light Gray", input >= 50 && input < 70)
                        .Add("White", input >= 70 && input <= 100)
                        .Add("Invalid", input < 0 || input > 100);
                }, ifThen =>
                {
                    ifThen.When("Black").WriteLine("That number is black");
                    ifThen.When("Dark Gray").WriteLine("That number is dark gray");
                    ifThen.When("Light Gray").WriteLine("That number is light gray");
                    ifThen.When("White").WriteLine("That number is white");
                    ifThen.When("Invalid").WriteLine("That number is invalid");
                })
                .WriteLine("Goodbye");
        }
    }
}