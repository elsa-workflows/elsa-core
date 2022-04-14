using Elsa.Samples.CompensationConsole.Workflows;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Create a service container with Elsa services.
var services = new ServiceCollection()
    .AddElsa(options => options
        .AddActivitiesFrom<Program>()
        .AddWorkflowsFrom<Program>())
    .BuildServiceProvider();

// Get a workflow runner.
var workflowRunner = services.GetRequiredService<IBuildsAndStartsWorkflow>();

// Run the workflow.
var result = await workflowRunner.BuildAndStartWorkflowAsync<CompensableWorkflow>();
//var result = await workflowRunner.BuildAndStartWorkflowAsync<FaultingWorkflow>();

var logger = services.GetRequiredService<ILogger<Program>>();
logger.LogDebug("@{Result}", result);