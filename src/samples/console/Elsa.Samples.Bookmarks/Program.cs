using Elsa.Extensions;
using Elsa.Samples.Bookmarks.Activities;
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

var workflow = new Workflow
{
    Root = new Sequence
    {
        Activities =
        {
            new WriteLine("Hello World!"),
            new MyEvent(), // Blocks until the event bookmark is resumed.
            new WriteLine("Event received!")
        }
    }
};

// Run the workflow.
var result = await workflowRunner.RunAsync(workflow);

// Resume the workflow using te created bookmark.
var bookmark = result.WorkflowState.Bookmarks.Single();
var workflowState = result.WorkflowState;
await workflowRunner.RunAsync(workflow, workflowState, new RunWorkflowOptions(bookmarkId: bookmark.Id));

