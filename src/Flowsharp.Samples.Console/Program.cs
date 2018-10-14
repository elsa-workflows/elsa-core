using System;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Activities;
using Flowsharp.Models;
using Flowsharp.Serialization;
using Flowsharp.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Flowsharp.Samples.Console
{
    class Program
    {
        static async Task Main()
        {
            var invoker = new WorkflowInvoker(new Logger<WorkflowInvoker>(new NullLoggerFactory()));

            var writeLine1 = new WriteLine("You have now transitioned into a networked workflow.");
            var writeLine2 = new WriteLine("Let's run a program.");
            var writeLine3 = new WriteLine("Enter first value:");
            var readLine1 = new ReadLine();
            var setVariable1 = new SetVariable("x", (w, a) => int.Parse((string) w.CurrentScope.ReturnValue));
            var writeLine4 = new WriteLine("Enter second value:");
            var readLine2 = new ReadLine();
            var setVariable2 = new SetVariable("y", (w, a) => int.Parse((string) w.CurrentScope.ReturnValue));
            var writeLine5 = new WriteLine((w, a) =>
            {
                var x = w.CurrentScope.GetVariable<int>("x");
                var y = w.CurrentScope.GetVariable<int>("y");
                var z = x + y;
                return $"{x} + {y} = {z}";
            });
            var writeLine6 = new WriteLine("Try again? (Y/N)");
            var readLine3 = new ReadLine();
            var setVariable3 = new SetVariable("tryAgain", (w, a) => string.Equals("y", (string)w.CurrentScope.ReturnValue, StringComparison.CurrentCultureIgnoreCase));
            var ifElse1 = new IfElse((w, a) => w.CurrentScope.GetVariable<bool>("tryAgain"));
            var writeLine7 = new WriteLine("Bye!");

            var activities = new IActivity[] { writeLine1, writeLine2, writeLine3, writeLine4, writeLine5, writeLine6, readLine1, readLine2, readLine3, setVariable1, setVariable2, setVariable3, ifElse1 };
            var connections = new[]
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
            
            var workflow = new Workflow(activities, connections);
            var workflowContext = await invoker.InvokeAsync(workflow);
            var serializer = new JsonWorkflowSerializer();
            var json = await serializer.SerializeAsync(workflow, CancellationToken.None);
        }
    }
}
