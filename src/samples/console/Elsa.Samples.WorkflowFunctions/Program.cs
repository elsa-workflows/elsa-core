using Elsa.Extensions;
using Elsa.Samples.WorkflowFunctions.Workflows;
using Elsa.Workflows.Core.Contracts;
using Microsoft.Extensions.DependencyInjection;

// Setup service container.
var services = new ServiceCollection();

// Add Elsa services.
services.AddElsa();

// Build service container.
var serviceProvider = services.BuildServiceProvider();

// Resolve a workflow runner to run the workflows.
var workflowRunner = serviceProvider.GetRequiredService<IWorkflowRunner>();

// AddWorkflow.
Console.WriteLine("=== AddWorkflow ===");
var addWorkflow = new AddWorkflow(4.4f, 5.6f);

// Run the workflow.
var result1 = await workflowRunner.RunAsync(addWorkflow);

Console.WriteLine("Result: {0}", result1.Result);

// AddInputsWorkflow:
Console.WriteLine("=== AddInputsWorkflow ===");
var input = new { a = 5.6, b = 9.4 }.ToDictionary();
var result2 = await workflowRunner.RunAsync<AddInputsWorkflow, float>(new RunWorkflowOptions(input: input));
Console.WriteLine("Result: {0}", result2);

// SumInputsWorkflow:
Console.WriteLine("=== SumInputsWorkflow ===");
input = new { numbers = new[]{ 9.6f, 2.5f, 1.2f, 4.8f } }.ToDictionary();
var result3 = await workflowRunner.RunAsync<SumInputsWorkflow, float>(new RunWorkflowOptions(input: input));
Console.WriteLine("Result: {0}", result3);