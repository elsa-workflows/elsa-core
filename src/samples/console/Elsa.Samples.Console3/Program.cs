using Elsa.Extensions;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;
using Microsoft.Extensions.DependencyInjection;

// Setup a service container from which we can resolve Elsa services.
var services = new ServiceCollection();

// Add Elsa services to the service collection.
services.AddElsa();

// Build a service provider.
var serviceProvider = services.BuildServiceProvider();

// Resolve a workflow runner.
var workflowRunner = serviceProvider.GetRequiredService<IWorkflowRunner>();

// Define a workflow.
//var workflow = new WriteLine("Hello World!");
var workflow = new Workflow
{
    Root = new Sequence
    {
        Activities =
        {
            new WriteLine("Hello World!"),
            new WriteLine("Goodbye cruel world..."),
        }
    }
};

// Execute the workflow.
workflowRunner.RunAsync(workflow);