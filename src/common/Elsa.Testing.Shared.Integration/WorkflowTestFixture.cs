using Elsa.Extensions;
using Elsa.Features.Services;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;
using Elsa.Workflows.State;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Testing.Shared;

/// <summary>
/// A test fixture for integration testing workflows and activities.
/// Provides a fluent API to configure services, run workflows, and capture output.
/// </summary>
public class WorkflowTestFixture
{
    private readonly TestApplicationBuilder _testApplicationBuilder;
    private IServiceProvider? _services;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowTestFixture"/> class.
    /// </summary>
    /// <param name="testOutputHelper">The test output helper</param>
    public WorkflowTestFixture(ITestOutputHelper testOutputHelper)
    {
        _testApplicationBuilder = new TestApplicationBuilder(testOutputHelper);
        CapturingTextWriter = new CapturingTextWriter();
        _testApplicationBuilder.WithCapturingTextWriter(CapturingTextWriter);
    }

    /// <summary>
    /// Gets the capturing text writer that captures standard output from WriteLine activities.
    /// </summary>
    public CapturingTextWriter CapturingTextWriter { get; }

    /// <summary>
    /// Gets the service provider. Throws if Build() hasn't been called yet.
    /// </summary>
    private IServiceProvider Services => _services ?? throw new InvalidOperationException("Build() must be called before accessing services");

    /// <summary>
    /// Configures Elsa features.
    /// </summary>
    /// <param name="configure">Action to configure Elsa</param>
    /// <returns>The fixture instance for method chaining</returns>
    [UsedImplicitly]
    public WorkflowTestFixture ConfigureElsa(Action<IModule> configure)
    {
        _testApplicationBuilder.ConfigureElsa(configure);
        return this;
    }

    /// <summary>
    /// Configures the service collection.
    /// </summary>
    /// <param name="configure">Action to configure the service collection</param>
    /// <returns>The fixture instance for method chaining</returns>
    [UsedImplicitly]
    public WorkflowTestFixture ConfigureServices(Action<IServiceCollection> configure)
    {
        _testApplicationBuilder.ConfigureServices(configure);
        return this;
    }

    /// <summary>
    /// Adds a workflow to the service provider.
    /// </summary>
    [UsedImplicitly]
    public WorkflowTestFixture AddWorkflow<T>() where T : IWorkflow
    {
        _testApplicationBuilder.AddWorkflow<T>();
        return this;
    }

    /// <summary>
    /// Adds activities from the assembly containing the specified type.
    /// </summary>
    [UsedImplicitly]
    public WorkflowTestFixture AddActivitiesFrom<T>()
    {
        _testApplicationBuilder.AddActivitiesFrom<T>();
        return this;
    }

    /// <summary>
    /// Adds workflows from the specified relative directory.
    /// </summary>
    /// <param name="directory">The path segments of the directory</param>
    [UsedImplicitly]
    public WorkflowTestFixture WithWorkflowsFromDirectory(params string[] directory)
    {
        _testApplicationBuilder.WithWorkflowsFromDirectory(directory);
        return this;
    }

    /// <summary>
    /// Builds the service provider and populates registries.
    /// Must be called before running workflows.
    /// </summary>
    public async Task<WorkflowTestFixture> BuildAsync()
    {
        _services = _testApplicationBuilder.Build();
        await Services.PopulateRegistriesAsync();
        return this;
    }

    /// <summary>
    /// Runs a workflow and returns the workflow result.
    /// Automatically builds the fixture if not already built.
    /// </summary>
    /// <param name="workflow">The workflow to run</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The workflow result after execution</returns>
    public async Task<RunWorkflowResult> RunWorkflowAsync(IWorkflow workflow, CancellationToken cancellationToken = default)
    {
        if (_services == null)
            await BuildAsync();

        var workflowRunner = Services.GetRequiredService<IWorkflowRunner>();
        return await workflowRunner.RunAsync(workflow, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Runs an activity wrapped in a workflow and returns the workflow result.
    /// Automatically builds the fixture if not already built.
    /// </summary>
    /// <param name="activity">The activity to run</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The workflow result after execution</returns>
    public async Task<RunWorkflowResult> RunActivityAsync(IActivity activity, CancellationToken cancellationToken = default)
    {
        if (_services == null)
            await BuildAsync();

        var workflowRunner = Services.GetRequiredService<IWorkflowRunner>();
        return await workflowRunner.RunAsync(activity, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Runs a workflow by definition ID and returns the workflow state.
    /// Automatically builds the fixture if not already built.
    /// </summary>
    /// <param name="definitionId">The workflow definition ID</param>
    /// <param name="input">Optional input dictionary</param>
    /// <returns>The workflow state after execution</returns>
    public async Task<WorkflowState> RunWorkflowAsync(string definitionId, IDictionary<string, object>? input = null)
    {
        if (_services == null)
            await BuildAsync();

        return await Services.RunWorkflowUntilEndAsync(definitionId, input);
    }

    /// <summary>
    /// Gets the outcomes produced by a specific activity from the workflow result.
    /// </summary>
    /// <param name="result">The workflow run result</param>
    /// <param name="activity">The activity to get outcomes for</param>
    /// <returns>Collection of outcome names</returns>
    public IEnumerable<string> GetOutcomes(RunWorkflowResult result, IActivity activity)
    {
        var activityContext = result.Journal.ActivityExecutionContexts
            .FirstOrDefault(c => c.Activity.Id == activity.Id);

        return activityContext?.GetOutcomes() ?? [];
    }

    /// <summary>
    /// Checks if a specific activity produced a specific outcome.
    /// </summary>
    /// <param name="result">The workflow run result</param>
    /// <param name="activity">The activity to check</param>
    /// <param name="outcome">The outcome name to check for</param>
    /// <returns>True if the activity produced the specified outcome</returns>
    public bool HasOutcome(RunWorkflowResult result, IActivity activity, string outcome)
    {
        return GetOutcomes(result, activity).Contains(outcome);
    }

    /// <summary>
    /// Gets the execution status of a specific activity from the workflow result.
    /// </summary>
    /// <param name="result">The workflow run result</param>
    /// <param name="activity">The activity to get status for</param>
    /// <returns>The activity status, or null if the activity wasn't found in the journal</returns>
    public ActivityStatus? GetActivityStatus(RunWorkflowResult result, IActivity activity)
    {
        var activityContext = result.Journal.ActivityExecutionContexts
            .FirstOrDefault(c => c.Activity.Id == activity.Id);

        return activityContext?.Status;
    }
}
