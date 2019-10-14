using Elsa;
using Elsa.Activities.Console.Activities;
using Elsa.Activities.ControlFlow;
using Elsa.Expressions;
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
                .StartWith<WriteLine>(x => x.TextExpression = new LiteralExpression("Welcome to Calculator Workflow!"), "welcome")
                .Then<WriteLine>(x => x.TextExpression = new LiteralExpression("Enter number 1:"), id: "enter-first-number-prompt")
                .Then<ReadLine>(x => x.VariableName = "number1", id: "read-first-number")
                .Then<WriteLine>(x => x.TextExpression = new LiteralExpression("Enter number 2:"), id: "enter-second-number-prompt")
                .Then<ReadLine>(x => x.VariableName = "number2", id: "read-second-number")
                .Then<WriteLine>(x => x.TextExpression = new LiteralExpression("Now enter the operation you wish to apply. Options are: add, subtract, multiply or divide:"), id: "enter-operation-prompt")
                .Then<ReadLine>(x => x.VariableName = "operation", id: "read-operation")
                .Then<Switch>(@switch =>
                    {
                        @switch.Expression = new JavaScriptExpression<string>("operation");
                        @switch.Cases = new[] { "add", "subtract", "multiply", "divide" };
                    },
                    @switch =>
                    {
                        @switch
                            .When("add")
                            .Then<Sum>(SetupOperation, id: "perform-sum")
                            .Then("show-result");
                        
                        @switch
                            .When("subtract")
                            .Then<Subtract>(SetupOperation, id: "perform-subtract")
                            .Then("show-result");
                        
                        @switch
                            .When("multiply")
                            .Then<Multiply>(SetupOperation, id: "perform-multiply")
                            .Then("show-result");
                        
                        @switch
                            .When("divide")
                            .Then<Divide>(SetupOperation, id: "perform-divide")
                            .Then("show-result");
                    },
                    "inspect-selected-operation"
                )
                .Add<WriteLine>(x => x.TextExpression = new JavaScriptExpression<string>("`Result: ${result}`"), "show-result")
                .Then<WriteLine>(x => x.TextExpression = new LiteralExpression("Try again? (y/n)"), id: "try-again-prompt")
                .Then<ReadLine>(x => x.VariableName = "retry", id: "read-try-again")
                .Then<IfElse>(
                    x => x.ConditionExpression = new JavaScriptExpression<bool>("retry.toLowerCase() === 'y'"),
                    ifElse =>
                    {
                        ifElse
                            .When(OutcomeNames.True)
                            .Then("enter-first-number-prompt");
                        
                        ifElse
                            .When(OutcomeNames.False)
                            .Then<WriteLine>(x => x.TextExpression = new LiteralExpression("Bye!"), id: "say-good-bye");
                    }, id: "inspect-retry");
        }

        private void SetupOperation(ArithmeticOperation operation)
        {
            operation.Values = new JavaScriptExpression<double[]>("[number1, number2]");
            operation.ResultVariableName = "result";
        }
    }
}