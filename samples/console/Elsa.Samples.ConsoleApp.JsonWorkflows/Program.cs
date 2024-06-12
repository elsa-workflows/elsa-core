using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Management;
using Microsoft.Extensions.DependencyInjection;

// Setup service container.
var services = new ServiceCollection();

// Add Elsa services.
services.AddElsa();

// Build service container.
var serviceProvider = services.BuildServiceProvider();

// Populate registries. This is only necessary for applications that are not using hosted services.
await serviceProvider.PopulateRegistriesAsync();

// Import a workflow from a JSON file.
var workflowJson = await File.ReadAllTextAsync("HelloWorld.json");

// Get a serializer to deserialize the workflow.
var serializer = serviceProvider.GetRequiredService<IWorkflowSerializer>();

// Deserialize the workflow.
var workflow = serializer.Deserialize(workflowJson);

// Resolve a workflow runner to run the workflow.
var workflowRunner = serviceProvider.GetRequiredService<IWorkflowRunner>();

// Run the workflow.
await workflowRunner.RunAsync(workflow);