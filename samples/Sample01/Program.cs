using System;
using System.Threading.Tasks;
using Elsa.Core.Extensions;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;
using Sample01.Activities;

namespace Sample01
{
    /// <summary>
    /// A minimal workflows program defined in code with a strongly-typed workflow class.
    /// </summary>
    class Program
    {
        static async Task Main(string[] args)
        {
            // Setup a service collection.
            var services = new ServiceCollection()
                .AddWorkflows()
                .AddActivity<HelloWorld>()
                .AddActivity<GoodByeWorld>()
                .BuildServiceProvider();

            // Invoke the workflow.
            var invoker = services.GetService<IWorkflowInvoker>();
            await invoker.InvokeAsync<HelloWorldWorkflow>();

            Console.ReadLine();
        }
    }
}