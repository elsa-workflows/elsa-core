using Elsa;
using Elsa.Activities.Console.Activities;
using Elsa.Core.Activities.Primitives;
using Elsa.Core.Expressions;
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
                .StartWith<WriteLine>(x => x.TextExpression = new PlainTextExpression("Welcome to Calculator Workflow!"))
                .Then<WriteLine>(x => x.TextExpression = new PlainTextExpression("Enter number 1:"), id: "start")
                .Then<ReadLine>(x => x.ArgumentName = "number1")
                .Then<WriteLine>(x => x.TextExpression = new PlainTextExpression("Enter number 2:"))
                .Then<ReadLine>(x => x.ArgumentName = "number2")
                .Then<WriteLine>(x => x.TextExpression = new PlainTextExpression("Now enter the operation you wish to apply. Options are: add, subtract, multiply or divide:"))
                .Then<ReadLine>(x => x.ArgumentName = "operation")
                .Then<Switch>(@switch =>
                    {
                        @switch.Expression = new JavaScriptExpression<string>("operation");
                        @switch.Cases = new[] { "add", "subtract", "multiply", "divide" };
                    },
                    @switch =>
                    {
                        @switch
                            .When("add")
                            .Then<Sum>(SetupOperation)
                            .Then("showResult");
                        
                        @switch
                            .When("subtract")
                            .Then<Subtract>(SetupOperation)
                            .Then("showResult");
                        
                        @switch
                            .When("multiply")
                            .Then<Multiply>(SetupOperation)
                            .Then("showResult");
                        
                        @switch
                            .When("divide")
                            .Then<Divide>(SetupOperation)
                            .Then("showResult");
                    }
                )
                .Add<WriteLine>(x => x.TextExpression = new JavaScriptExpression<string>("`Result: ${result}`"), "showResult")
                .Then<WriteLine>(x => x.TextExpression = new PlainTextExpression("Try again? (y/n)"))
                .Then<ReadLine>(x => x.ArgumentName = "retry")
                .Then<IfElse>(
                    x => x.ConditionExpression = new JavaScriptExpression<bool>("retry.toLowerCase() === 'y'"),
                    ifElse =>
                    {
                        ifElse
                            .When(OutcomeNames.True)
                            .Then("start");
                        
                        ifElse
                            .When(OutcomeNames.False)
                            .Then<WriteLine>(x => x.TextExpression = new PlainTextExpression("Bye!"));
                    });;
        }

        private void SetupOperation(ArithmeticOperation operation)
        {
            operation.Values = new JavaScriptExpression<double[]>("[number1, number2]");
            operation.ResultVariableName = "result";
        }
    }
}