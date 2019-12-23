using Elsa;
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
                .StartWith<WriteLine>(x => x.Text = new LiteralExpression<string>("Welcome to Calculator Workflow!")).WithDisplayName("Welcome").WithDescription("Display a Welcome message.")
                .SetVariable("exit", new JavaScriptExpression<bool>("false")).WithDisplayName("Initialize Exit Variable")
                .While(new JavaScriptExpression<bool>("!exit"),
                    whileActivity => whileActivity
                        .When(OutcomeNames.Iterate)
                        .Then<WriteLine>(x => x.Text = new LiteralExpression<string>("Enter number 1:")).WithDisplayName("Enter First Number").WithDescription("Tell user to enter first number.")
                        .Then<ReadLine>().WithName("Number1").WithDisplayName("Await First Number").WithDescription("Wait for the user to enter first number.")
                        .Then<WriteLine>(x => x.Text = new LiteralExpression<string>("Enter number 2:")).WithDisplayName("Enter Second Number").WithDescription("Tell user to enter second number.")
                        .Then<ReadLine>().WithName("Number2").WithDisplayName("Await Second Number").WithDescription("Wait for the user to enter second number.")
                        .Then<WriteLine>(x => x.Text = new LiteralExpression<string>("Now enter the operation you wish to apply. Options are: add, subtract, multiply or divide:")).WithDisplayName("Prompt Operation").WithDescription("Prompt user enter an operation.")
                        .Then<ReadLine>().WithName("Operation").WithDisplayName("Await Operation").WithDescription("Wait for the user to enter the operation to perform.")
                        .Switch(
                            new JavaScriptExpression<string>("(Operation)"),
                            new[] { "add", "subtract", "multiply", "divide" },
                            @switch =>
                            {
                                @switch
                                    .When("add")
                                    .Then<Sum>(SetupOperation).WithName("PerformSum")
                                    .SetVariable("Result", x => x.Input.GetValue<double>())
                                    .Then("ShowResult");

                                @switch
                                    .When("subtract")
                                    .Then<Subtract>(SetupOperation).WithName("PerformSubtract")
                                    .SetVariable("Result", x => x.Input.GetValue<double>())
                                    .Then("ShowResult");

                                @switch
                                    .When("multiply")
                                    .Then<Multiply>(SetupOperation).WithName("PerformMultiply")
                                    .SetVariable("Result", x => x.Input.GetValue<double>())
                                    .Then("ShowResult");

                                @switch
                                    .When("divide")
                                    .Then<Divide>(SetupOperation).WithName("PerformDivide")
                                    .SetVariable("Result", x => x.Input.GetValue<double>())
                                    .Then("ShowResult");
                            }
                        ).WithName("InspectSelectedOperation").WithDisplayName("Check Operation").WithDescription("Checking which operation was selected.")
                        .Add<WriteLine>(x => x.Text = new JavaScriptExpression<string>("`Result: ${Result}`")).WithName("ShowResult").WithDisplayName("Show Result").WithDescription("Show the result of the operation")
                        .Then<WriteLine>(x => x.Text = new LiteralExpression<string>("Try again? (y/n)")).WithDisplayName("Prompt Try Again").WithDescription("Prompt the user to try again or quit.")
                        .Then<ReadLine>().WithName("TryAgain").WithDisplayName("Await Try Again").WithDescription("Wait for the user to respond whether they want to try again.")
                        .SetVariable("exit", new JavaScriptExpression<bool>("TryAgain.Input !== 'y' && TryAgain.Input !== 'Y'")).WithDisplayName("Set Exit Variable").WithDescription("Set the Exit variable to the received response.")
                        .Then(whileActivity)).WithDisplayName("While Exit is False").WithDescription("Loop while Exit is false.")
                .Then<WriteLine>(x => x.Text = new LiteralExpression<string>("Bye!")).WithDisplayName("Say Goodbye").WithDescription("Display a Goodbye message.");
        }

        private void SetupOperation(ArithmeticOperation operation)
        {
            operation.Values = new JavaScriptExpression<double[]>("[Number1, Number2]");
        }
    }
}