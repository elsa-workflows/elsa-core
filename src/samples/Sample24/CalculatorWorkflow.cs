using System;
using Elsa;
using Elsa.Activities.ControlFlow.Activities;
using Elsa.Expressions;
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
                .Then(context => context.SetVariable("Exit", false))
            .While(
                x => x.Condition = new CodeExpression<bool>(context => !context.GetVariable<bool>("Exit")),
                whileActivity =>
                {
                    whileActivity
                        .When(OutcomeNames.Iterate)
                        .Then(() => WriteLine("Enter number 1:"))
                        .Then(context => context.SetVariable("Number1", double.Parse(ReadLine())))
                        .Then(() => WriteLine("Enter number 2:"))
                        .Then(context => context.SetVariable("Number2", double.Parse(ReadLine())))
                        .Then(() => WriteLine("Now enter the operation you wish to apply. Options are: add, subtract, multiply or divide:"))
                        .Then(context => context.SetVariable("Operation", ReadLine()))
                        .Switch(
                            @switch =>
                            {
                                @switch.Value = new CodeExpression<string>(context => context.GetVariable<string>("Operation"));
                                @switch.Cases = new[] { "add", "subtract", "multiply", "divide" };
                            },
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
                        .Then(context => context.SetVariable("Exit", ReadLine() != "y"))
                        .Then(whileActivity);
                })
                .Then(() => WriteLine("Bye!"));
        }

        private void Calculate(WorkflowExecutionContext context, Func<double, double, double> formula)
        {
            var number1 = context.GetVariable<double>("Number1");
            var number2= context.GetVariable<double>("Number2");
            var result = formula(number1, number2);
            context.SetVariable("Result", result);
        }
        
        private void WriteLine(string value) => Console.WriteLine(value);
        private string ReadLine() => Console.ReadLine();
    }
}