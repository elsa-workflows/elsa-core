using Elsa.Extensions;
using Elsa.Samples.LoopingWorkflows.Workflows;
using Elsa.Workflows.Core.Contracts;
using Microsoft.Extensions.DependencyInjection;

// Setup service container.
var services = new ServiceCollection();

// Add Elsa services.
services.AddElsa();

// Build service container.
var serviceProvider = services.BuildServiceProvider();

// Resolve a workflow runner to run the workflow.
var workflowRunner = serviceProvider.GetRequiredService<IWorkflowRunner>();

// Run the Hello workflow.
await workflowRunner.RunUntilEndAsync(HelloWorkflow.Create());

// Run the For workflow.
await workflowRunner.RunUntilEndAsync(ForWorkflow.Create());

// Run the For Each workflow.
await workflowRunner.RunUntilEndAsync(ForEachWorkflow.Create());

// Run the While workflow.
await workflowRunner.RunUntilEndAsync(WhileWorkflow.Create());