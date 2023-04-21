using Elsa.Extensions;
using Elsa.Samples.CustomActivities.Activities;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using Microsoft.Extensions.DependencyInjection;

// Setup service container.
var services = new ServiceCollection();

// Add Elsa services.
services.AddElsa();

// Build service container.
var serviceProvider = services.BuildServiceProvider();

// Resolve a workflow runner to run the workflow.
var workflowRunner = serviceProvider.GetRequiredService<IWorkflowRunner>();

// Create a workflow.
var a = new Variable<int>();
var b = new Variable<int>();
var sum = new Variable<int>();

var workflow = new Workflow
{
    Root = new Sequence
    {
        Variables = {a, b, sum},
        Activities =
        {
            new WriteLine("Enter first value"),
            new ReadLine(a),
            new WriteLine("Enter second value"),
            new ReadLine(b),
            new Sum(a, b, sum),
            new WriteLine(context => $"The sum of {a.Get(context)} and {b.Get(context)} is {sum.Get(context)}")
        }
    }
};

// Run the workflow.
await workflowRunner.RunAsync(workflow);