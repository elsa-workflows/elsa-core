using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa;
using Elsa.Activities.Console.Activities;
using Elsa.Activities.Console.Extensions;
using Elsa.Builders;
using Elsa.Core.Expressions;
using Elsa.Core.Extensions;
using Elsa.Core.Serialization.Formatters;
using Elsa.Expressions;
using Elsa.Extensions;
using Elsa.Persistence;
using Elsa.Persistence.FileSystem.Extensions;
using Elsa.Serialization.Formatters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace PersistenceProgram
{
    internal class Program
    {
        private static async Task Main()
        {
            // Setup configuration for the file system stores.
            var rootDir = "workflows"; //Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location), "workflows");
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Workflows:Directory"] = rootDir,
                    ["Workflows:Format"] = YamlTokenFormatter.FormatName
                })
                .Build();

            // Setup a service collection and use the FileSystemProvider for both workflow definitions and workflow instances.
            var services = new ServiceCollection()
                .AddWorkflowsInvoker()
                .AddConsoleActivities()
                .AddFileSystemWorkflowDefinitionStoreProvider(configuration.GetSection("Workflows"))
                .AddFileSystemWorkflowInstanceStoreProvider(configuration.GetSection("Workflows"))
                .AddSingleton(Console.In)
                .BuildServiceProvider();

            // Define a workflow.
            var workflowDefinition = new WorkflowBuilder()
                .Add(new WriteLine("Hi! What's your name?"))
                .Next(new ReadLine("name"))
                .Next(new WriteLine(new WorkflowExpression<string>(JavaScriptEvaluator.SyntaxName, "`Nice to meet you, ${name}!`")))
                .BuildWorkflow();

            // Save the workflow definition using IWorkflowDefinitionStore.
            var workflowDefinitionStore = services.GetService<IWorkflowDefinitionStore>();
            await workflowDefinitionStore.SaveAsync(workflowDefinition, CancellationToken.None);

            // Load the workflow definition.
            workflowDefinition = await workflowDefinitionStore.GetByIdAsync(workflowDefinition.Id, CancellationToken.None);

            // Invoke the workflow.
            var invoker = services.GetService<IWorkflowInvoker>();
            var workflowExecutionContext = await invoker.InvokeAsync(workflowDefinition, workflowDefinition.Activities.First());

            // Save the workflow instance using IWorkflowInstanceStore.
            var workflowInstance = workflowExecutionContext.Workflow;
            var workflowInstanceStore = services.GetService<IWorkflowInstanceStore>();
            await workflowInstanceStore.SaveAsync(workflowInstance, CancellationToken.None);

            Console.ReadLine();
        }
    }
}