using System;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.Activities;
using Flowsharp.Builders;
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

            var workflow = new WorkflowBuilder()
                .AddActivity(new WriteLine("You have now transitioned into a networked workflow."), helloWorld => 
                    helloWorld.Connect(new WriteLine("Let's run a program."), runProgram => 
                        runProgram.Connect(new WriteLine("Enter first value:"), firstValue => 
                            firstValue.Connect(new ReadLine(), value1 => 
                                value1.Connect(new SetVariable("x", (w, a) => int.Parse((string)w.CurrentScope.ReturnValue)), setX => 
                                    setX.Connect(new WriteLine("Enter second value:"), secondValue => 
                                        secondValue.Connect(new ReadLine(), value2 =>
                                            value2.Connect(new SetVariable("y", (w, a) => int.Parse((string)w.CurrentScope.ReturnValue)), setY => 
                                            setY.Connect(new WriteLine((w, a) =>
                                            {
                                                var x = w.CurrentScope.GetVariable<int>("x");
                                                var y = w.CurrentScope.GetVariable<int>("y");
                                                var z = x + y;
                                                return $"{x} + {y} = {z}";
                                            }))))))))))
                .Build();
            
            await invoker.InvokeAsync(workflow);
            
            var serializer = new JsonWorkflowSerializer();
            var json = await serializer.SerializeAsync(workflow, CancellationToken.None);
        }
    }
}
