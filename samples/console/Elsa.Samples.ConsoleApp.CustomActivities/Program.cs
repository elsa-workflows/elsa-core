using Elsa.Extensions;
using Elsa.Samples.ConsoleApp.CustomActivities.Activities;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Setup service container.
var services = new ServiceCollection();
services.AddLogging(logging => logging.AddConsole());

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
            new PrintMessage
            {
                Message = "Enter first value"
            },
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