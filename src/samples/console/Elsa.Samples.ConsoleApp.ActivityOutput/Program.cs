using Elsa.Extensions;
using Elsa.Samples.ConsoleApp.ActivityOutput.Workflows;
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

// Run workflows.
await workflowRunner.RunAsync<TargetActivityOutputWorkflow>();
await workflowRunner.RunAsync<LastResultWorkflow>();