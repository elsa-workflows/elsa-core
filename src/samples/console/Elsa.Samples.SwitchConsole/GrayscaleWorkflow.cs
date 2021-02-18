using System;
using Elsa.Activities.Console;
using Elsa.Activities.ControlFlow;
using Elsa.Builders;

namespace Elsa.Samples.SwitchConsole
{
    public class GrayscaleWorkflow : IWorkflow
    {
        private readonly Random _random;

        public GrayscaleWorkflow()
        {
            _random = new Random();
        }

        public void Build(IWorkflowBuilder builder)
        {
            builder
                .WriteLine("--Grayscale Calculator--").WithName("Start")
                .WriteLine("Enter a number between 0 and 100.")
                .ReadLine()
                .Switch(cases =>
                    {
                        var input = cases.Context.GetInput<int>();
                        cases
                            .Add("Black", input >= 0 && input < 20)
                            .Add("Dark Gray", input >= 20 && input < 50)
                            .Add("Light Gray", input >= 50 && input < 70)
                            .Add("Gray", input >= 20 && input < 70)
                            .Add("White", input >= 70 && input <= 100)
                            .Add("Invalid", input < 0 || input > 100);
                    },
                    SwitchMode.MatchFirst,
                    @case =>
                    {
                        @case.When("Black").WriteLine("That number is black");
                        @case.When("Dark Gray").WriteLine("That number is dark gray");
                        @case.When("Light Gray").WriteLine("That number is light gray");
                        @case.When("Gray").WriteLine("That number is gray");
                        @case.When("White").WriteLine("That number is white");
                        @case.When("Invalid").WriteLine("That number is invalid");
                    })
                .WriteLine("Goodbye");
        }
    }
}