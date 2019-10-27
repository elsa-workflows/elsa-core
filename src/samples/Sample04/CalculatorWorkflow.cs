using Elsa;
using Elsa.Activities;
using Elsa.Activities.Console.Activities;
using Elsa.Activities.ControlFlow;
using Elsa.Activities.ControlFlow.Activities;
using Elsa.Expressions;
using Elsa.Scripting.JavaScript;
using Elsa.Services;
using Elsa.Services.Models;
using Sample04.Activities;

namespace Sample04
{
    public class CalculatorWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .StartWith<WriteLine>(x => x.TextExpression = new LiteralExpression("Welcome to Calculator Workflow!")).WithDisplayName("Welcome")
                .Then<SetVariable>(
                    x =>
                    {
                        x.VariableName = "Exit";
                        x.ValueExpression = new JavaScriptExpression<bool>("false");
                    }).WithDisplayName("Initialize Exit Variable")
                .Then<While>(
                    x => x.ConditionExpression = new JavaScriptExpression<bool>("!Exit"),
                    whileActivity =>
                    {
                        whileActivity
                            .When(OutcomeNames.Iterate)
                            .Then<WriteLine>(x => x.TextExpression = new LiteralExpression("Enter number 1:"))
                            .Then<ReadLine>(x => x.VariableName = "number1")
                            .Then<WriteLine>(x => x.TextExpression = new LiteralExpression("Enter number 2:"))
                            .Then<ReadLine>(x => x.VariableName = "number2")
                            .Then<WriteLine>(x => x.TextExpression = new LiteralExpression("Now enter the operation you wish to apply. Options are: add, subtract, multiply or divide:"))
                            .Then<ReadLine>(x => x.VariableName = "operation")
                            .Then<Switch>(
                                @switch =>
                                {
                                    @switch.Expression = new JavaScriptExpression<string>("operation");
                                    @switch.Cases = new[] { "add", "subtract", "multiply", "divide" };
                                },
                                @switch =>
                                {
                                    @switch
                                        .When("add")
                                        .Then<Sum>(SetupOperation).WithName("PerformSum")
                                        .Then("ShowResult");

                                    @switch
                                        .When("subtract")
                                        .Then<Subtract>(SetupOperation).WithName("PerformSubtract")
                                        .Then("ShowResult");

                                    @switch
                                        .When("multiply")
                                        .Then<Multiply>(SetupOperation).WithName("PerformMultiply")
                                        .Then("ShowResult");

                                    @switch
                                        .When("divide")
                                        .Then<Divide>(SetupOperation).WithName("PerformDivide")
                                        .Then("ShowResult");
                                }
                            ).WithName("InspectSelectedOperation")
                            .Add<WriteLine>(x => x.TextExpression = new JavaScriptExpression<string>("`Result: ${result}`")).WithName("ShowResult")
                            .Then<WriteLine>(x => x.TextExpression = new LiteralExpression("Try again? (y/n)"))
                            .Then<ReadLine>().WithName("TryAgain")
                            .Then<SetVariable>(x =>
                            {
                                x.VariableName = "Exit";
                                x.ValueExpression = new JavaScriptExpression<bool>("TryAgain.Input !== 'y' && TryAgain.Input !== 'Y'");
                            })
                            .Then(whileActivity);
                    }).WithDisplayName("While")
                .Then<WriteLine>(x => x.TextExpression = new LiteralExpression("Bye!"));
        }

        private void SetupOperation(ArithmeticOperation operation)
        {
            operation.Values = new JavaScriptExpression<double[]>("[number1, number2]");
            operation.ResultVariableName = "result";
        }
    }
}