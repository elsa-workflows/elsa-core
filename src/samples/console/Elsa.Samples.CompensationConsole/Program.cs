using Elsa.Samples.CompensationConsole;
using Elsa.Samples.CompensationConsole.Workflows;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;

// Create a service container with Elsa services.
var services = new ServiceCollection()
    .AddElsa(options => options
        .AddActivitiesFrom<Program>()
        .AddWorkflowsFrom<Program>())
    .BuildServiceProvider();

// Get a workflow runner.
var workflowRunner = services.GetRequiredService<IBuildsAndStartsWorkflow>();

// Run the workflow.
await workflowRunner.BuildAndStartWorkflowAsync<CompensableWorkflow>();