using System;
using Elsa.Activities.Console;
using Elsa.Activities.ControlFlow;
using Elsa.Builders;

namespace Elsa.Samples.IfElseConsole
{
    public class HappinessWorkflow : IWorkflow
    {
        private readonly Random _random;

        public HappinessWorkflow()
        {
            _random = new Random();
        }

        public void Build(IWorkflowBuilder workflow)
        {
            workflow
                .WriteLine("--POND OF HAPPINESS--", "Start")
                .WriteLine("Throw some Rupees in and your wishes will surely come true.")
                .WriteLine("Do you want to throw Rupees?")
                .ReadLine()
                .IfElse(
                    activity => activity.WithCondition(context => IsYes(context.Input)),
                    ifElse =>
                    {
                        ifElse
                            .When(IfElse.True)
                            .WriteLine(GetCurse)
                            .WriteLine("...")
                            .Then("Start");

                        ifElse
                            .When(IfElse.False)
                            .WriteLine("Bye.");
                    });
        }

        private static bool IsYes(object? value)
        {
            var text = ((string)value!).ToLowerInvariant();
            return text switch
            {
                "yes" => true,
                "y" => true,
                "sure" => true,
                _ => false
            };
        }

        private string GetCurse()
        {
            var curses = new[]
            {
                "Today, you will have Great Luck",
                "Today, you will have a Little Luck",
                "Today, you will have a Big Trouble"
            };

            var index = _random.Next(0, 3);
            return curses[index];
        }
    }
}