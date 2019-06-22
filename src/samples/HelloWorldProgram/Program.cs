using System;
using System.Threading.Tasks;
using Elsa.Activities.Console.Activities;
using Elsa.Activities.Console.Extensions;
using Elsa.Core.Expressions;
using Elsa.Core.Extensions;
using Elsa.Core.Services.WorkflowBuilders;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;

namespace HelloWorldProgram
{
    /// <summary>
    /// A minimal workflows program.
    /// </summary>
    class Program
    {
        static async Task Main(string[] args)
        {
            // Setup a service collection.
            var services = new ServiceCollection()
                .AddWorkflowBuilder()
                .AddWorkflowsCore()
                .AddConsoleActivities()
                .BuildServiceProvider();

            // Create a workflow.
            var workflow = services.GetService<WorkflowBuilder>()
                .Add<WriteLine>(x => x.TextExpression = PlainText.Expression<string>("Hello World!"))
                .Connect<WriteLine>(x => x.TextExpression = PlainText.Expression<string>("Goodbye!"))
                .Build();

            // Invoke the workflow.
            var invoker = services.GetService<IWorkflowInvoker>();
            var workflowContext = await invoker.InvokeAsync(workflow);

            // Output: Hello World!

            // Serialize the workflow.
//            var serializer = services.GetRequiredService<IWorkflowSerializer>();
//            var data = serializer.Serialize(workflowContext.WorkflowInstance, JsonTokenFormatter.FormatName);

//            Console.WriteLine("Serialized workflow instance:");
//            Console.WriteLine(data);

            // Deserialize the workflow.
//            workflow = serializer.Deserialize(data, JsonTokenFormatter.FormatName);
            //await invoker.InvokeAsync(workflow);

            Console.ReadLine();
        }
    }
}