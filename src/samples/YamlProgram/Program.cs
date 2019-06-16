using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa;
using Elsa.Activities.Console.Extensions;
using Elsa.Core.Extensions;
using Elsa.Core.Serialization.Formatters;
using Elsa.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace YamlProgram
{
    internal class Program
    {
        private static async Task Main()
        {
            // Setup a service collection and use the FileSystemProvider for both workflow definitions and workflow instances.
            var services = new ServiceCollection()
                .AddWorkflowsInvoker()
                .AddConsoleActivities()
                .AddSingleton(Console.In)
                .BuildServiceProvider();
            
            // Load the data and specify data format.
            var data = Resources.SampleWorkflowDefinition;
            var format = YamlTokenFormatter.FormatName;

            // Deserialize the workflow from data.
            var serializer = services.GetService<IWorkflowSerializer>();
            var workflowDefinition = await serializer.DeserializeAsync(data, format, CancellationToken.None);
            
            // Invoke the workflow.
            var invoker = services.GetService<IWorkflowInvoker>();
            await invoker.InvokeAsync(workflowDefinition, workflowDefinition.Activities.First());
            
            Console.ReadLine();
        }
    }
}