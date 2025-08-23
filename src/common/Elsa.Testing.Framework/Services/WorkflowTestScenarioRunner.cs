using Elsa.Testing.Framework.Middleware.Activities;
using Elsa.Testing.Framework.Models;
using Elsa.Workflows;
using Elsa.Workflows.Management;
using Elsa.Workflows.Middleware.Activities;
using Elsa.Workflows.Middleware.Workflows;
using Elsa.Workflows.Options;
using Elsa.Workflows.Pipelines.ActivityExecution;
using Elsa.Workflows.Pipelines.WorkflowExecution;

namespace Elsa.Testing.Framework.Services;

public class WorkflowTestScenarioRunner(IWorkflowDefinitionService workflowDefinitionService, IWorkflowRunner workflowRunner, IServiceProvider serviceProvider)
{
    public async Task<TestResult> RunAsync(Scenario scenario, CancellationToken cancellationToken = default)
    {
        var workflowGraph = await workflowDefinitionService.FindWorkflowGraphAsync(scenario.WorkflowDefinitionHandle, cancellationToken);

        if (workflowGraph is null)
            throw new InvalidOperationException($"Workflow graph with handle {scenario.WorkflowDefinitionHandle} not found.");

        var workflowExecutionPipeline = new WorkflowExecutionPipeline(serviceProvider, builder =>
        {
            builder.UseExceptionHandling();
            builder.UseDefaultActivityScheduler();
        });

        var activityExecutionPipelineBuilder = new ActivityExecutionPipeline(serviceProvider, builder =>
        {
            builder.UseExceptionHandling();
            builder.UseMiddleware<ActivityExecutionTracerMiddleware>();
            builder.UseDefaultActivityInvoker();
        });

        var runOptions = new RunWorkflowOptions
        {
            Input = scenario.Input,
            Variables = scenario.Variables,
            WorkflowExecutionPipeline = workflowExecutionPipeline,
            ActivityExecutionPipeline = activityExecutionPipelineBuilder
        };

        var runResult = await workflowRunner.RunAsync(workflowGraph, runOptions, cancellationToken);
        var assertionResults = new List<AssertionResult>();
        var assertionContext = new AssertionContext
        {
            RunWorkflowResult = runResult,
            CancellationToken = cancellationToken,
            ServiceProvider = serviceProvider,
        };

        foreach (var assertion in scenario.Assertions)
        {
            var assertionResult = await assertion.RunAsync(assertionContext);
            assertionResults.Add(assertionResult);
        }

        var resultStatus = assertionResults.All(x => x.Passed) ? TestResultStatus.Passed : TestResultStatus.Failed;
        return new(resultStatus, assertionResults);
    }
}