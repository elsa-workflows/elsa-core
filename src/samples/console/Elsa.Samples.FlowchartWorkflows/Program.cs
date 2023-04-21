using Elsa.Extensions;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Activities.Flowchart.Activities;
using Elsa.Workflows.Core.Activities.Flowchart.Models;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using Microsoft.Extensions.DependencyInjection;

// Setup service container.
var services = new ServiceCollection();

// Add Elsa services.
services.AddElsa();

// Build service container.
var serviceProvider = services.BuildServiceProvider();

// Define a workflow variable to capture the output of the ReadLine activity.
var nameVariable = new Variable<string>();

// Create a workflow.
var writeLine1 = new WriteLine("Please tell me your name:");
var writeLine2 = new ReadLine(nameVariable);
var writeLine3 = new WriteLine(context => $"Nice to meet you, {nameVariable.Get(context)}!");

var workflow = new Flowchart
{
    // Register the name variable.
    Variables = { nameVariable }, 
    
    // Add the activities.
    Activities =
    {
        writeLine1, 
        writeLine2,
        writeLine3
    },
    
    // Setup the connections between activities.
    Connections =
    {
        new Connection(writeLine1, writeLine2),
        new Connection(writeLine2, writeLine3)
    }
};

// Resolve a workflow runner to run the workflow.
var workflowRunner = serviceProvider.GetRequiredService<IWorkflowRunner>();

// Run the workflow.
await workflowRunner.RunAsync(workflow);