using System;
using System.Threading.Tasks;
using Elsa.Activities.Console.Extensions;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;
using Sample17.Activities;

namespace Sample17
{
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            var services = BuildServices();
            var workflowInvoker = services.GetRequiredService<IWorkflowInvoker>();

            await workflowInvoker.StartAsync<CreateDocumentWorkflow>();
        }
        
        private static IServiceProvider BuildServices()
        {
            return new ServiceCollection()
                .AddWorkflows()
                .AddWorkflow<CreateDocumentWorkflow>()
                .AddConsoleActivities()
                .AddActivity<CreateDocument>()
                .BuildServiceProvider();
        }
    }
}