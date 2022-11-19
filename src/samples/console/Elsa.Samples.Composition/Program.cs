using Elsa.Extensions;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;
using Microsoft.Extensions.DependencyInjection;

// Setup service container.
var services = new ServiceCollection();

// Add Elsa services.
services.AddElsa();

// Build service container.
var serviceProvider = services.BuildServiceProvider();

// Declare a workflow variable for use in the workflow.
var ageVariable = new Variable<int>();

// Declare a workflow.
var workflow = new Sequence
{
    Variables = { ageVariable },
    Activities =
    {
        new AskAge
        {
            Prompt = new Input<string>("What's your age?"),
            Result = new Output(ageVariable)
        },
        new WriteLine(context => $"The age you told me is: {ageVariable.Get(context)}")
    }
};

// Resolve a workflow runner to run the workflow.
var workflowRunner = serviceProvider.GetRequiredService<IWorkflowRunner>();

// Run the workflow.
await workflowRunner.RunAsync(workflow);