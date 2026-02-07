using Elsa.Common.Models;
using Elsa.Testing.Shared.Models;
using Elsa.Testing.Shared.Services;
using Elsa.Workflows.ComponentTests.Fixtures;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.ComponentTests.Abstractions;

[Collection(nameof(AppCollection))]
public abstract class AppComponentTest(App app) : IDisposable
{
    protected WorkflowServer WorkflowServer { get; } = app.WorkflowServer;
    protected Cluster Cluster { get; } = app.Cluster;
    protected Infrastructure Infrastructure { get; } = app.Infrastructure;
    protected IServiceScope Scope { get; } = app.WorkflowServer.Services.CreateScope();

    void IDisposable.Dispose()
    {
        Scope.Dispose();
        OnDispose();
    }

    protected virtual void OnDispose()
    {
    }

    /// <summary>
    /// Runs a workflow by definition ID and waits for completion.
    /// </summary>
    protected async Task<TestWorkflowExecutionResult> RunWorkflowAsync(string definitionId)
    {
        var runner = Scope.ServiceProvider.GetRequiredService<AsyncWorkflowRunner>();
        return await runner.RunAndAwaitWorkflowCompletionAsync(
            WorkflowDefinitionHandle.ByDefinitionId(definitionId, VersionOptions.Published));
    }

    /// <summary>
    /// Asserts that the workflow finished successfully.
    /// </summary>
    protected static void AssertWorkflowFinished(TestWorkflowExecutionResult result)
    {
        if (result.WorkflowExecutionContext.SubStatus != WorkflowSubStatus.Finished)
            throw new Xunit.Sdk.XunitException($"Expected workflow to be Finished but was {result.WorkflowExecutionContext.SubStatus}");
    }

    /// <summary>
    /// Gets all WriteLine activity execution records from the result.
    /// </summary>
    protected static List<ActivityExecutionRecord> GetWriteLineRecords(TestWorkflowExecutionResult result) =>
        result.ActivityExecutionRecords.Where(x => x.ActivityType == "Elsa.WriteLine").ToList();

    /// <summary>
    /// Gets the text output from WriteLine activity execution records.
    /// </summary>
    protected static List<string?> GetWriteLineMessages(TestWorkflowExecutionResult result) =>
        GetWriteLineRecords(result)
            .Select(x => x.ActivityState?[nameof(Elsa.Workflows.Activities.WriteLine.Text)]?.ToString())
            .ToList();
}