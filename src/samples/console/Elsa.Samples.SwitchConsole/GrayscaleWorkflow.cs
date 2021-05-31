using System;
using Elsa.Activities.Console;
using Elsa.Activities.ControlFlow;
using Elsa.ActivityResults;
using Elsa.Builders;
using Elsa.Services;
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

    public class ForkBranchDecisionActivity : Activity
    {
        protected override IActivityExecutionResult OnExecute()
        {
            Console.WriteLine("Choose a branch. A or B");
            Console.WriteLine("Typing 'a' will result in going through branch A");
            Console.WriteLine("Any other key will result in branch B");

            var userChosenBranch = Console.ReadLine()!;

            if (userChosenBranch.Equals("A", StringComparison.InvariantCultureIgnoreCase))
                return Outcome("A");

            return Outcome("B");
        }
    }

    public class SimpleForkWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .WriteLine("This demonstrates a simple workflow with switch.")
                .WriteLine("Using switch we can branch a workflow.")
                .Then<ForkBranchDecisionActivity>(fork =>
                {
                    fork.When("A")
                        .WriteLine("You are in A branch. First line")
                        .WriteLine("You are in A branch. Second line.")
                        .ThenNamed("Finish");

                    fork.When("B")
                        .WriteLine("You are in B branch. First line")
                        .WriteLine("You are in B branch. Second line.")
                        .ThenNamed("Finish");
                    
                })
                .WriteLine("Workflow finished.").WithName("Finish");
        }
    }
}