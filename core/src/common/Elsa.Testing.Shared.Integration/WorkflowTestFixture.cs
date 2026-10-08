using Elsa.Common.Multitenancy;
using Elsa.Expressions.Models;
using Elsa.Features.Services;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using Elsa.Workflows.Options;
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
        _testApplicationBuilder = new(testOutputHelper);
        CapturingTextWriter = new();
        _testApplicationBuilder.WithCapturingTextWriter(CapturingTextWriter);
    }

    /// <summary>
    /// Gets the capturing text writer that captures standard output from WriteLine activities.
    /// </summary>
    public CapturingTextWriter CapturingTextWriter { get; }

    /// <summary>
    /// Gets the service provider. Throws if Build() hasn't been called yet.
    /// </summary>
    public IServiceProvider Services => _services ?? throw new InvalidOperationException("Build() must be called before accessing services");

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
        if (_services != null)
            return this;
        
        _services = _testApplicationBuilder.Build();
        var tenantService = Services.GetRequiredService<ITenantService>();
        await tenantService.ActivateTenantsAsync();
        
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
        await BuildAsync();
        var workflowRunner = Services.GetRequiredService<IWorkflowRunner>();
        return await workflowRunner.RunAsync(workflow, cancellationToken: cancellationToken);
    }
    
    /// <summary>
    /// Runs the specified workflow and returns the workflow result.
    /// Automatically builds the fixture if not already built.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The workflow result after execution</returns>
    public async Task<RunWorkflowResult> RunWorkflowAsync<TWorkflow>(CancellationToken cancellationToken = default) where TWorkflow : IWorkflow, new()
    {
        await BuildAsync();
        var workflowRunner = Services.GetRequiredService<IWorkflowRunner>();
        return await workflowRunner.RunAsync<TWorkflow>(cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Runs a workflow with the specified options and returns the workflow result.
    /// Automatically builds the fixture if not already built.
    /// </summary>
    /// <param name="workflow">The workflow to run</param>
    /// <param name="options">Workflow execution options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The workflow result after execution</returns>
    public async Task<RunWorkflowResult> RunWorkflowAsync(IWorkflow workflow, RunWorkflowOptions options, CancellationToken cancellationToken = default)
    {
        await BuildAsync();
        var workflowRunner = Services.GetRequiredService<IWorkflowRunner>();
        return await workflowRunner.RunAsync(workflow, options, cancellationToken);
    }
    
    /// <summary>
    /// Runs the specified workflow with the specified options and returns the workflow result.
    /// Automatically builds the fixture if not already built.
    /// </summary>
    /// <param name="options">Workflow execution options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The workflow result after execution</returns>
    public async Task<RunWorkflowResult> RunWorkflowAsync<TWorkflow>(RunWorkflowOptions options, CancellationToken cancellationToken = default) where TWorkflow : IWorkflow, new()
    {
        await BuildAsync();
        var workflowRunner = Services.GetRequiredService<IWorkflowRunner>();
        return await workflowRunner.RunAsync<TWorkflow>(options, cancellationToken);
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
        await BuildAsync();
        var workflowRunner = Services.GetRequiredService<IWorkflowRunner>();
        return await workflowRunner.RunAsync(activity, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Runs an activity wrapped in a workflow with the specified options and returns the workflow result.
    /// Automatically builds the fixture if not already built.
    /// </summary>
    /// <param name="activity">The activity to run</param>
    /// <param name="options">Workflow execution options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The workflow result after execution</returns>
    public async Task<RunWorkflowResult> RunActivityAsync(IActivity activity, RunWorkflowOptions options, CancellationToken cancellationToken = default)
    {
        await BuildAsync();
        var workflowRunner = Services.GetRequiredService<IWorkflowRunner>();
        return await workflowRunner.RunAsync(activity, options, cancellationToken);
    }

    /// <summary>
    /// Runs a workflow by definition ID and returns the workflow state.
    /// Automatically builds the fixture if not already built.
    /// </summary>
    /// <param name="definitionId">The workflow definition ID</param>
    /// <param name="input">Optional input dictionary</param>
    /// <param name="options">Optional workflow execution options</param>
    /// <returns>The workflow state after execution</returns>
    public async Task<WorkflowState> RunWorkflowAsync(string definitionId, IDictionary<string, object>? input = null, RunWorkflowOptions? options = null)
    {
        await BuildAsync();
        return await Services.RunWorkflowUntilEndAsync(definitionId, input, runWorkflowOptions: options);
    }

    public async Task<WorkflowDefinition> ImportWorkflowDefinitionAsync(string fileName)
    {
        await BuildAsync();
        return await Services.ImportWorkflowDefinitionAsync(fileName);
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

    /// <summary>
    /// Creates a WorkflowExecutionContext for testing.
    /// This creates a minimal workflow execution context without executing the workflow.
    /// </summary>
    /// <param name="variables">Optional workflow variables to include in the workflow</param>
    /// <returns>A WorkflowExecutionContext that can be used for testing</returns>
    public async Task<WorkflowExecutionContext> CreateWorkflowExecutionContextAsync(Variable[]? variables = null)
    {
        if (_services == null)
            await BuildAsync();

        // Create a minimal workflow with variables
        var workflow = new Workflow
        {
            Root = new Sequence()
        };

        if (variables != null)
            foreach (var variable in variables)
                workflow.Variables.Add(variable);

        // Build the workflow graph
        var workflowGraphBuilder = Services.GetRequiredService<IWorkflowGraphBuilder>();
        var workflowGraph = await workflowGraphBuilder.BuildAsync(workflow);

        // Create workflow execution context
        return await WorkflowExecutionContext.CreateAsync(
            Services,
            workflowGraph,
            $"test-instance-{Guid.NewGuid()}",
            CancellationToken.None
        );
    }

    /// <summary>
    /// Creates an ActivityExecutionContext for testing.
    /// Creates a workflow execution context first, then creates an activity execution context for the specified activity.
    /// </summary>
    /// <param name="activity">The activity to create a context for. If null, uses the workflow itself.</param>
    /// <param name="variables">Optional workflow variables to include</param>
    /// <returns>An ActivityExecutionContext that can be used for testing</returns>
    public async Task<ActivityExecutionContext> CreateActivityExecutionContextAsync(IActivity? activity = null, Variable[]? variables = null)
    {
        var workflowExecutionContext = await CreateWorkflowExecutionContextAsync(variables);
        return await CreateActivityExecutionContextAsync(workflowExecutionContext, activity);
    }

    /// <summary>
    /// Creates an ActivityExecutionContext for testing using an existing WorkflowExecutionContext.
    /// </summary>
    /// <param name="workflowExecutionContext">The workflow execution context to use</param>
    /// <param name="activity">The activity to create a context for. If null, uses the workflow itself.</param>
    /// <returns>An ActivityExecutionContext that can be used for testing</returns>
    public async Task<ActivityExecutionContext> CreateActivityExecutionContextAsync(WorkflowExecutionContext workflowExecutionContext, IActivity? activity = null)
    {
        // Use the workflow itself if no activity specified, as Workflow implements IVariableContainer.
        // This ensures variables are accessible.
        var targetActivity = activity ?? workflowExecutionContext.Workflow;
        return await workflowExecutionContext.CreateActivityExecutionContextAsync(targetActivity);
    }

    /// <summary>
    /// Creates an ExpressionExecutionContext for testing expression evaluation.
    /// This creates a minimal workflow and activity execution context, then initializes variables.
    /// Variables are properly registered and accessible via dynamic accessors (e.g., getMyVariable, setMyVariable).
    /// </summary>
    /// <param name="variables">Optional workflow variables to include in the execution context</param>
    /// <returns>An ExpressionExecutionContext that can be used for expression evaluation</returns>
    public async Task<ExpressionExecutionContext> CreateExpressionExecutionContextAsync(Variable[]? variables = null)
    {
        var activityContext = await CreateActivityExecutionContextAsync(activity: null, variables: variables);
        return await CreateExpressionExecutionContextAsync(activityContext, variables);
    }

    /// <summary>
    /// Creates an ExpressionExecutionContext using an existing ActivityExecutionContext.
    /// Initializes variables if provided.
    /// </summary>
    /// <param name="activityContext">The activity execution context to use</param>
    /// <param name="variables">Optional workflow variables to initialize</param>
    /// <returns>An ExpressionExecutionContext that can be used for expression evaluation</returns>
    public Task<ExpressionExecutionContext> CreateExpressionExecutionContextAsync(ActivityExecutionContext activityContext, Variable[]? variables = null)
    {
        // Initialize variables in the execution context if provided
        // Use Variable.Set() to properly register variables (same approach as ActivityTestFixture)
        if (variables != null)
            foreach (var variable in variables) 
                variable.Set(activityContext.ExpressionExecutionContext, variable.Value);

        return Task.FromResult(activityContext.ExpressionExecutionContext);
    }
}
