using Elsa.Extensions;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Setup service container.
var services = new ServiceCollection();

// Add Elsa services.
services.AddElsa();

// Configure logging.
services.AddLogging(logging => logging.AddConsole().SetMinimumLevel(LogLevel.Debug));

// Build service container.
var serviceProvider = services.BuildServiceProvider();

// Create a workflow.
var workflow = new Sequence
{
    Activities =
    {
        new WriteLine("Hello World!"), 
        new WriteLine("Goodbye cruel world...")
    }
};

// Resolve a workflow runner to run the workflow.
var workflowRunner = serviceProvider.GetRequiredService<IWorkflowRunner>();

// Run the workflow.
await workflowRunner.RunAsync(workflow);