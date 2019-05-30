using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Elsa;
using Elsa.Activities.Console.Activities;
using Elsa.Activities.Console.Extensions;
using Elsa.Activities.Primitives.Activities;
using Elsa.Activities.Primitives.Extensions;
using Elsa.Builders;
using Elsa.Expressions;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.FileSystem.Extensions;
using Elsa.Serialization.Formatters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace PersistenceProgram
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Setup configuration.
            var rootDir = Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location), "workflows");
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Workflows:Directory"] = rootDir,
                    ["Workflows:Format"] = YamlTokenFormatter.FormatName
                })
                .Build();

            // Setup a service collection and use the FileSystemProvider for both workflow definitions and workflow instances.
            var services = new ServiceCollection()
                .AddWorkflowsInvoker()
                .AddPrimitiveActivities()
                .AddConsoleActivities()
                .AddFileSystemWorkflowDefinitionStoreProvider(configuration.GetSection("Workflows"))
                .AddFileSystemWorkflowInstanceStoreProvider(configuration.GetSection("Workflows"))
                .AddSingleton(Console.In)
                .BuildServiceProvider();

            // Define a workflow.
            var workflowDefinition = CreateSampleWorkflow();
            
            // Save the workflow definition.
            var workflowDefinitionStore = services.GetService<IWorkflowDefinitionStore>();
            await workflowDefinitionStore.SaveAsync(workflowDefinition, CancellationToken.None);
            
            // Load the workflow definition.
            workflowDefinition = await workflowDefinitionStore.GetByIdAsync(workflowDefinition.Id, CancellationToken.None);

            // Invoke the workflow.
            var invoker = services.GetService<IWorkflowInvoker>();
            var workflowExecutionContext = await invoker.InvokeAsync(workflowDefinition, workflowDefinition.Activities.First());
            
            // Save the workflow instance.
            var workflowInstanceStore = services.GetService<IWorkflowInstanceStore>();
            await workflowInstanceStore.SaveAsync(workflowExecutionContext.Workflow, CancellationToken.None);

            Console.ReadLine();
        }

        private static Workflow CreateSampleWorkflow()
        {
            // 1. Ask two numbers
            // 2. Ask operation to apply to the two numbers.
            // 3. Show the result of the calculation.
            // 4. Ask user to try again or exit program.

            return new WorkflowBuilder()
                .Add(new WriteLine("Please enter a number:") { Alias = "Start" })
                .Next(new ReadLine("a"))
                .Next(new WriteLine("Please enter another number:"))
                .Next(new ReadLine("b"))
                .Next(new WriteLine("Please enter the operation you would like to perform on the two numbers. Allowed operations:\r\n-Add\r\n-Subtract\r\n-Divide\r\n-Multiply"))
                .Next(new ReadLine("op"))
                .Next(new Switch(JavaScriptEvaluator.SyntaxName, "op"), @switch =>
                {
                    @switch.Next(new SetVariable("result", JavaScriptEvaluator.SyntaxName, "parseFloat(a) + parseFloat(b)"), "Add").Next("ShowResult");
                    @switch.Next(new SetVariable("result", JavaScriptEvaluator.SyntaxName, "a - b"), "Subtract").Next("ShowResult");
                    @switch.Next(new SetVariable("result", JavaScriptEvaluator.SyntaxName, "a * b"), "Multiply").Next("ShowResult");
                    @switch.Next(new SetVariable("result", JavaScriptEvaluator.SyntaxName, "a / b"), "Divide").Next("ShowResult");
                })
                .Next(new WriteLine(new WorkflowExpression<string>(JavaScriptEvaluator.SyntaxName, "`Result: ${result}`")) { Alias = "ShowResult" })
                .Next(new WriteLine("Try again? (Y/N)"))
                .Next(new ReadLine("retry"))
                .Next(new IfElse(new WorkflowExpression<bool>(JavaScriptEvaluator.SyntaxName, "retry.toLowerCase() === 'y'")), ifElse =>
                {
                    ifElse.Next("Start", "True");
                    ifElse.Next(new WriteLine("Bye!"), "False");
                })
                .BuildWorkflow();
        }
    }
}