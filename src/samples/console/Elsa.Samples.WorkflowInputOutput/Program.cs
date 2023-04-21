using Elsa.Extensions;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;
using Microsoft.Extensions.DependencyInjection;

// Setup service container.
var services = new ServiceCollection();

// Add Elsa services.
services.AddElsa();

// Build service container.
var serviceProvider = services.BuildServiceProvider();

// Create a workflow that echoes some message it receives via input.

var workflow = new WriteLine(context => $"Echo: {context.GetInput<string>("Message")}!");

// Resolve a workflow runner to run the workflow.
var workflowRunner = serviceProvider.GetRequiredService<IWorkflowRunner>();

// Create an input dictionary.
var input = new Dictionary<string, object>
{
    ["Message"] = "Hello World!"
};

// Run the workflow and provide the input.
await workflowRunner.RunAsync(workflow, new RunWorkflowOptions(input: input));

// Create a workflow that returns some output.
var workflow2 = new Inline(context => context.WorkflowExecutionContext.Output["Message"] = "Hello from workflow!");

// Run the workflow and hold on to its workflow state.
var result = await workflowRunner.RunAsync(workflow2);

// Get the output.
var output = result.WorkflowState.Output["Message"];

Console.WriteLine($"Output: {output}");