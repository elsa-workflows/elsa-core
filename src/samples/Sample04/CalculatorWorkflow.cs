using Elsa;
using Elsa.Activities;
using Elsa.Activities.Console.Activities;
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
                .StartWith<WriteLine>(x => x.TextExpression = new LiteralExpression("Welcome to Calculator Workflow!"))
                .WithDisplayName("Welcome")
                .WithDescription("Display a Welcome message.")
                .Then<SetVariable>(
                    x =>
                    {
                        x.VariableName = "exit";
                        x.ValueExpression = new JavaScriptExpression<bool>("false");
                    })
                .WithDisplayName("Initialize Exit Variable")
                .Then<While>(
                    x => x.ConditionExpression = new JavaScriptExpression<bool>("!exit"),
                    whileActivity =>
                    {
                        whileActivity
                            .When(OutcomeNames.Iterate)
                            .Then<WriteLine>(x => x.TextExpression = new LiteralExpression("Enter number 1:")).WithDisplayName("Enter First Number").WithDescription("Tell user to enter first number.")
                            .Then<ReadLine>(x => x.VariableName = "number1").WithDisplayName("Await First Number").WithDescription("Wait for the user to enter first number.")
                            .Then<WriteLine>(x => x.TextExpression = new LiteralExpression("Enter number 2:")).WithDisplayName("Enter Second Number").WithDescription("Tell user to enter second number.")
                            .Then<ReadLine>(x => x.VariableName = "number2").WithDisplayName("Await Second Number").WithDescription("Wait for the user to enter second number.")
                            .Then<WriteLine>(x => x.TextExpression = new LiteralExpression("Now enter the operation you wish to apply. Options are: add, subtract, multiply or divide:")).WithDisplayName("Prompt Operation").WithDescription("Prompt user enter an operation.")
                            .Then<ReadLine>(x => x.VariableName = "operation").WithDisplayName("Await Operation").WithDescription("Wait for the user to enter the operation to perform.")
                            .Then<Switch>(
                                @switch =>
                                {
                                    @switch.Expression = new JavaScriptExpression<string>("(operation)");
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
                            ).WithName("InspectSelectedOperation").WithDisplayName("Check Operation").WithDescription("Checking which operation was selected.")
                            .Add<WriteLine>(x => x.TextExpression = new JavaScriptExpression<string>("`Result: ${result}`")).WithName("ShowResult").WithDisplayName("Show Result").WithDescription("Show the result of the operation")
                            .Then<WriteLine>(x => x.TextExpression = new LiteralExpression("Try again? (y/n)")).WithDisplayName("Prompt Try Again").WithDescription("Prompt the user to try again or quit.")
                            .Then<ReadLine>().WithName("TryAgain").WithDisplayName("Await Try Again").WithDescription("Wait for the user to respond whether they want to try again.")
                            .Then<SetVariable>(x =>
                            {
                                x.VariableName = "exit";
                                x.ValueExpression = new JavaScriptExpression<bool>("TryAgain.Input !== 'y' && TryAgain.Input !== 'Y'");
                            }).WithDisplayName("Set Exit Variable").WithDescription("Set the Exit variable to the received response.")
                            .Then(whileActivity);
                    }).WithDisplayName("While Exit is False").WithDescription("Loop while Exit is false.")
                .Then<WriteLine>(x => x.TextExpression = new LiteralExpression("Bye!")).WithDisplayName("Say Goodbye").WithDescription("Display a Goodbye message.");
        }

        private void SetupOperation(ArithmeticOperation operation)
        {
            operation.Values = new JavaScriptExpression<double[]>("[number1, number2]");
            operation.ResultVariableName = "result";
        }
    }
}