using System;
using Elsa.Activities.Console;
using Elsa.Activities.ControlFlow;
using Elsa.Builders;
using Elsa.Services.Models;

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
                .Switch(cases => cases
                    .Add(context => GetNumber(context) >= 0 && GetNumber(context) < 20, @case => @case.WriteLine("That number is black"))
                    .Add(context => GetNumber(context) >= 20 && GetNumber(context) < 50, @case => @case.WriteLine("That number is dark gray"))
                    .Add(context => GetNumber(context) >= 50 && GetNumber(context) < 70, @case => @case.WriteLine("That number is light gray"))
                    .Add(context => GetNumber(context) >= 20 && GetNumber(context) < 70, @case => @case.WriteLine("That number is gray"))
                    .Add(context => GetNumber(context) >= 70 && GetNumber(context) <= 100, @case => @case.WriteLine("That number is white"))
                    .Add(context => GetNumber(context) < 0 || GetNumber(context) > 100, @case => @case.WriteLine("That number is invalid"))
                )
                .WriteLine("Thanks for playing!");
        }

        private static int GetNumber(ActivityExecutionContext context) => context.GetInput<int>();
    }
}