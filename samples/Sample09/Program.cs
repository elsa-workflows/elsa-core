using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Elsa.Activities.Console.Extensions;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Persistence.Memory;
using Elsa.Serialization;
using Elsa.Serialization.Formatters;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Sample09
{
    /// <summary>
    /// A simple console program that loads & executes a workflow designed with the HTML5 workflow designer.
    /// </summary>
    class Program
    {
        static async Task Main()
        {
            var json = await ReadEmbeddedResourceAsync("Sample09.calculator.json");
            var services = BuildServices();
            var serializer = services.GetRequiredService<IWorkflowSerializer>();
            var workflow = serializer.Deserialize<WorkflowDefinitionVersion>(json, JsonTokenFormatter.FormatName);
            var invoker = services.GetRequiredService<IWorkflowInvoker>();
            
            await invoker.InvokeAsync(workflow);
        }

        private static IServiceProvider BuildServices()
        {
            return new ServiceCollection()
                .AddWorkflows()
                .AddConsoleActivities()
                .AddMemoryWorkflowDefinitionStore()
                .AddMemoryWorkflowInstanceStore()
                .BuildServiceProvider();
        }

        private static async Task<string> ReadEmbeddedResourceAsync(string resourceName)
        {
            var assembly = typeof(Program).GetTypeInfo().Assembly;
            using (var reader = new StreamReader(assembly.GetManifestResourceStream(resourceName)))
            {
                return await reader.ReadToEndAsync();
            }
        }
    }
}