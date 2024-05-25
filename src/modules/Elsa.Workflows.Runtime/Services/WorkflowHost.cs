using Elsa.Workflows.Activities;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Management;
using Elsa.Workflows.Models;
using Elsa.Workflows.Options;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Parameters;
using Elsa.Workflows.Runtime.Results;
using Elsa.Workflows.State;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Runtime.Services;

/// <summary>
/// Represents a host of a workflow instance to interact with.
/// </summary>
public class WorkflowHost : IWorkflowHost
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IIdentityGenerator _identityGenerator;
    private readonly ILogger<WorkflowHost> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkflowHost"/> class.
    /// </summary>
    public WorkflowHost(
        WorkflowGraph workflowGraph,
        WorkflowState workflowState,
        IIdentityGenerator identityGenerator,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<WorkflowHost> logger)
    {
        WorkflowGraph = workflowGraph;
        WorkflowState = workflowState;
        _serviceScopeFactory = serviceScopeFactory;
        _identityGenerator = identityGenerator;
        _logger = logger;
    }

    /// <inheritdoc />
    public WorkflowGraph WorkflowGraph { get; }

    /// <inheritdoc />
    public Workflow Workflow => WorkflowGraph.Workflow;

    /// <inheritdoc />
    public WorkflowState WorkflowState { get; set; }

    /// <inheritdoc />
    public async Task<bool> CanStartWorkflowAsync(StartWorkflowHostParams? @params = default, CancellationToken cancellationToken = default)
    {
        var strategyType = Workflow.Options.ActivationStrategyType;

        if (strategyType == null)
            return true;

        using var scope = _serviceScopeFactory.CreateScope();
        var instantiationStrategies = scope.ServiceProvider.GetServices<IWorkflowActivationStrategy>();
        var strategy = instantiationStrategies.FirstOrDefault(x => x.GetType() == strategyType);

        if (strategy == null)
            return true;

        var correlationId = @params?.CorrelationId;
        var strategyContext = new WorkflowInstantiationStrategyContext(Workflow, correlationId, cancellationToken);
        return await strategy.GetAllowActivationAsync(strategyContext);
    }

    /// <inheritdoc />
    public async Task<StartWorkflowHostResult> StartWorkflowAsync(StartWorkflowHostParams? @params = default, CancellationToken cancellationToken = default)
    {
        var parentWorkflowInstanceId = @params?.ParentWorkflowInstanceId;
        var correlationId = @params?.CorrelationId;
        var instanceId = @params?.InstanceId ?? _identityGenerator.GenerateId();
        var originalBookmarks = WorkflowState.Bookmarks.ToList();
        var input = @params?.Input;
        var properties = @params?.Properties;

        var runOptions = new RunWorkflowOptions
        {
            WorkflowInstanceId = instanceId,
            ParentWorkflowInstanceId = parentWorkflowInstanceId,
            CorrelationId = correlationId,
            Input = input,
            Properties = properties,
            TriggerActivityId = @params?.TriggerActivityId,
            StatusUpdatedCallback = @params?.StatusUpdatedCallback,
            CancellationTokens = @params?.CancellationTokens ?? cancellationToken
        };

        using var scope = _serviceScopeFactory.CreateScope();
        var workflowRunner = scope.ServiceProvider.GetRequiredService<IWorkflowRunner>();
        var workflowResult = @params?.IsExistingInstance == true
            ? await workflowRunner.RunAsync(WorkflowGraph, WorkflowState, runOptions, cancellationToken)
            : await workflowRunner.RunAsync(WorkflowGraph, runOptions, cancellationToken);

        WorkflowState = workflowResult.WorkflowState;

        var updatedBookmarks = WorkflowState.Bookmarks;
        var diff = Diff.For(originalBookmarks, updatedBookmarks);

        return new StartWorkflowHostResult(diff);
    }

    /// <summary>
    /// Resumes the <see cref="Workflow"/>.
    /// </summary>
    public async Task<ResumeWorkflowHostResult> ResumeWorkflowAsync(ResumeWorkflowHostParams? @params = default, CancellationToken cancellationToken = default)
    {
        var originalBookmarks = WorkflowState.Bookmarks.ToList();

        if (WorkflowState.Status != WorkflowStatus.Running)
        {
            _logger.LogWarning("Attempt to resume workflow {WorkflowInstanceId} that is not in the Running state. The actual state is {ActualWorkflowStatus}", WorkflowState.Id, WorkflowState.Status);
            return new ResumeWorkflowHostResult(Diff.For(originalBookmarks, new List<Bookmark>()));
        }

        var instanceId = WorkflowState.Id;
        var input = @params?.Input;

        var runOptions = new RunWorkflowOptions
        {
            WorkflowInstanceId = instanceId,
            CorrelationId = @params?.CorrelationId,
            BookmarkId = @params?.BookmarkId,
            ActivityId = @params?.ActivityId,
            ActivityNodeId = @params?.ActivityNodeId,
            ActivityInstanceId = @params?.ActivityInstanceId,
            ActivityHash = @params?.ActivityHash,
            Input = input,
            Properties = @params?.Properties,
            CancellationTokens = @params?.CancellationTokens ?? cancellationToken
        };

        using var scope = _serviceScopeFactory.CreateScope();
        var workflowRunner = scope.ServiceProvider.GetRequiredService<IWorkflowRunner>();
        var workflowResult = await workflowRunner.RunAsync(WorkflowGraph, WorkflowState, runOptions, cancellationToken);

        WorkflowState = workflowResult.WorkflowState;

        var updatedBookmarks = WorkflowState.Bookmarks;
        return new ResumeWorkflowHostResult(Diff.For(originalBookmarks, updatedBookmarks));
    }

    /// <inheritdoc />
    public async Task PersistStateAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var workflowInstanceManager = scope.ServiceProvider.GetRequiredService<IWorkflowInstanceManager>();
        await workflowInstanceManager.SaveAsync(WorkflowState, cancellationToken);
    }
}
