using Elsa.Extensions;
using Elsa.Samples.ConsoleApp.OutboundHttpRequests.Workflows;
using Elsa.Workflows.Core.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

// Setup service container.
var services = new ServiceCollection();

// Manually construct configuration. Normally this is provided by the host builder, but this is a simple Console app.
var config = new ConfigurationBuilder().Build();
services.AddSingleton<IConfiguration>(config);

// Add Elsa services.
services.AddElsa(elsa => elsa.UseHttp());

// Build service container.
var serviceProvider = services.BuildServiceProvider();

// Resolve a workflow runner to run the workflow.
var workflowRunner = serviceProvider.GetRequiredService<IWorkflowRunner>();

// Run the workflow.
await workflowRunner.RunAsync<GetUsersWorkflow>();