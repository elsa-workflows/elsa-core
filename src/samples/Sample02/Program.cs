using System;
using System.Threading.Tasks;
using Elsa.Activities.Console.Activities;
using Elsa.Activities.Console.Extensions;
using Elsa.Expressions;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Sample02
{
    /// <summary>
    /// A minimal workflows program defined in code using fluent workflow builder and Console activities.
    /// </summary>
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            // Setup a service collection.
            var services = new ServiceCollection()
                .AddElsa()
                .AddConsoleActivities()
                .BuildServiceProvider();

            // Define a workflow.
            var workflowBuilderFactory = services.GetRequiredService<Func<IWorkflowBuilder>>();
            var workflowBuilder = workflowBuilderFactory();
            var workflowBlueprint = workflowBuilder
                .StartWith<WriteLine>(x => x.Text = new CodeExpression<string>(() => "Hello world!"))
                .Then(() => Console.WriteLine("Look, custom code!"))
                .Then<WriteLine>(x => x.Text = new LiteralExpression<string>("Goodbye cruel world..."))
                .Build();

            // Start the workflow.
            var invoker = services.GetService<IWorkflowRunner>();
            await invoker.RunAsync(workflowBlueprint);

            Console.ReadLine();
        }
    }
}