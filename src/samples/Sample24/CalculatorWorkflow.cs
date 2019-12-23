using System;
using Elsa;
using Elsa.Activities.ControlFlow.Activities;
using Elsa.Services;
using Elsa.Services.Models;

namespace Sample24
{
    public class CalculatorWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .StartWith(() => WriteLine("Welcome to Calculator Workflow!"))
                .SetVariable("Exit", () => false)
                .While(context => !context.GetVariable<bool>("Exit"),
                    whileActivity =>
                    {
                        whileActivity
                            .When(OutcomeNames.Iterate)
                            .Then(() => WriteLine("Enter number 1:"))
                            .SetVariable("Number1", () => double.Parse(ReadLine()))
                            .Then(() => WriteLine("Enter number 2:"))
                            .SetVariable("Number2", () => double.Parse(ReadLine()))
                            .Then(() => WriteLine("Now enter the operation you wish to apply. Options are: add, subtract, multiply or divide:"))
                            .SetVariable("Operation", ReadLine)
                            .Switch(
                                context => context.GetVariable<string>("Operation"),
                                new[] { "add", "subtract", "multiply", "divide" },
                                @switch =>
                                {
                                    @switch
                                        .When("add")
                                        .Then(context => Calculate(context, (a, b) => a + b))
                                        .Then("ShowResult");

                                    @switch
                                        .When("subtract")
                                        .Then(context => Calculate(context, (a, b) => a - b))
                                        .Then("ShowResult");

                                    @switch
                                        .When("multiply")
                                        .Then(context => Calculate(context, (a, b) => a * b))
                                        .Then("ShowResult");

                                    @switch
                                        .When("divide")
                                        .Then(context => Calculate(context, (a, b) => a / b))
                                        .Then("ShowResult");
                                })
                            .Add(context => WriteLine($"Result: {context.GetVariable<double>("Result")}")).WithName("ShowResult")
                            .Then(() => WriteLine("Try again? y/n"))
                            .Then(context => context.SetVariable("Exit", !string.Equals(ReadLine().ToLowerInvariant(), "y", StringComparison.InvariantCultureIgnoreCase)))
                            .Then(whileActivity);
                    })
                .Then(() => WriteLine("Bye!"));
        }

        private void Calculate(ActivityExecutionContext context, Func<double, double, double> formula)
        {
            var workflowContext = context.WorkflowExecutionContext;
            var number1 = workflowContext.GetVariable<double>("Number1");
            var number2 = workflowContext.GetVariable<double>("Number2");
            var result = formula(number1, number2);
            workflowContext.SetVariable("Result", result);
        }

        private void WriteLine(string value) => Console.WriteLine(value);
        private string ReadLine() => Console.ReadLine();
    }
}