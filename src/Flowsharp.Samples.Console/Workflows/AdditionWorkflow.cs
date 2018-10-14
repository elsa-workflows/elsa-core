using System;
using Flowsharp.Activities;
using Flowsharp.Models;
using Flowsharp.Samples.Console.Activities;
using Flowsharp.Scripting;

namespace Flowsharp.Samples.Console.Workflows
{
    public class AdditionWorkflow : Workflow
    {
        public AdditionWorkflow()
        {
            var scriptEvaluator = new JintEvaluator();
            var writeLine1 = new WriteLine("Welcome to Addition Workflow!");
            var writeLine2 = new WriteLine("Let's run an interactive program.");
            var writeLine3 = new WriteLine("Enter first value:");
            var readLine1 = new ReadLine();
            var setVariable1 = new SetVariable(scriptEvaluator){ VariableName = "x", ValueExpression = "workflow.getLastResult()"};
            var writeLine4 = new WriteLine("Enter second value:");
            var readLine2 = new ReadLine();
            var setVariable2 = new SetVariable(scriptEvaluator){ VariableName = "y", ValueExpression = "workflow.getLastResult()"};
            var writeLine5 = new WriteLine((w, a) =>
            {
                var x = w.CurrentScope.GetVariable<int>("x");
                var y = w.CurrentScope.GetVariable<int>("y");
                var z = x + y;
                return $"{x} + {y} = {z}";
            });
            var writeLine6 = new WriteLine("Try again? (Y/N)");
            var readLine3 = new ReadLine();
            var setVariable3 = new SetVariable(scriptEvaluator){ VariableName = "tryAgain", ValueExpression = "'y' === workflow.getLastResult().toLowerCase()"};
            var ifElse1 = new IfElse(scriptEvaluator){ ConditionExpression = "workflow.getVariable('tryAgain')"};
            var writeLine7 = new WriteLine("Bye!");

            Activities = new IActivity[] { writeLine1, writeLine2, writeLine3, writeLine4, writeLine5, writeLine6, readLine1, readLine2, readLine3, setVariable1, setVariable2, setVariable3, ifElse1 };
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