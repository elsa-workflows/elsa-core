using Flowsharp.Activities;
using Flowsharp.Activities.Console.Activities;
using Flowsharp.Activities.Primitives.Activities;
using Flowsharp.Expressions;
using Flowsharp.Models;

namespace Flowsharp.Samples.Console.Workflows
{
    public class AdditionWorkflowLongRunning : Workflow
    {
        public AdditionWorkflowLongRunning()
        {
            var writeLine1 = new WriteLine("Welcome to Addition Workflow - Long Running edition!");
            var writeLine2 = new WriteLine("Let's run a long-running program.");
            var writeLine3 = new WriteLine("Enter first value:");
            var readLine1 = new ReadLine { ArgumentName = "x"};
            var setVariable1 = new SetVariable{ VariableName = "x", ValueExpression = new WorkflowExpression<object>(JavaScriptEvaluator.SyntaxName, "workflow.getLastResult()")};
            var writeLine4 = new WriteLine("Enter second value:");
            var readLine2 = new ReadLine { ArgumentName = "y"};
            var setVariable2 = new SetVariable{ VariableName = "y", ValueExpression = new WorkflowExpression<object>(JavaScriptEvaluator.SyntaxName, "workflow.getLastResult()")};
            var writeLine5 = new WriteLine {TextExpression = new WorkflowExpression<string>(JavaScriptEvaluator.SyntaxName, 
                "var x = workflow.getVariable('x');\r\n" + 
                "var y = workflow.getVariable('y');\r\n" + 
                "var sum = parseInt(x) + parseInt(y);\r\n" +
                "x + ' + ' + y + ' = ' + sum;"
            )};
            var writeLine6 = new WriteLine("Try again? (Y/N)");
            var readLine3 = new ReadLine { ArgumentName = "tryAgain"};
            var setVariable3 = new SetVariable{ VariableName = "tryAgain", ValueExpression = new WorkflowExpression<object>(JavaScriptEvaluator.SyntaxName, "'y' === workflow.getLastResult().toLowerCase()")};
            var ifElse1 = new IfElse{ ConditionExpression = new WorkflowExpression<bool>(JavaScriptEvaluator.SyntaxName, "workflow.getVariable('tryAgain')")};
            var writeLine7 = new WriteLine("Bye!");

            Activities = new IActivity[] { writeLine1, writeLine2, writeLine3, writeLine4, writeLine5, writeLine6, writeLine7, readLine1, readLine2, readLine3, setVariable1, setVariable2, setVariable3, ifElse1 };
            Connections = new[]
            {
                new Connection(writeLine1, writeLine2),
                new Connection(writeLine2, writeLine3),
                new Connection(writeLine3, readLine1),
                new Connection(readLine1, setVariable1),
                new Connection(setVariable1, writeLine4),
                new Connection(writeLine4, readLine2),
                new Connection(readLine2, setVariable2),
                new Connection(setVariable2, writeLine5),
                new Connection(writeLine5, writeLine6),
                new Connection(writeLine6, readLine3),
                new Connection(readLine3, setVariable3),
                new Connection(setVariable3, ifElse1),
                new Connection(ifElse1, "True", writeLine3),
                new Connection(ifElse1, "False", writeLine7),
            };
        }
    }
}