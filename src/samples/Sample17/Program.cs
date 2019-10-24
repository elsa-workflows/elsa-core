using System;
using System.Threading.Tasks;
using Elsa.Activities.Console.Extensions;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;
using Sample17.Activities;
using Sample17.Models;

namespace Sample17
{
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            var services = BuildServices();
            var workflowInvoker = services.GetRequiredService<IWorkflowInvoker>();
            var workflow = await workflowInvoker.StartAsync<CreateDocumentWorkflow>();
            var document = workflow.CurrentScope.GetVariable<Document>("Document");
            
            Console.WriteLine("Created document: {0}", document);
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