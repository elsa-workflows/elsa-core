using Elsa.Workflows.Activities;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Models;
using Elsa.Workflows.Options;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Options;
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
        IServiceScopeFactory serviceScopeFactory,
        Workflow workflow,
        WorkflowState workflowState,
        IIdentityGenerator identityGenerator,
        ILogger<WorkflowHost> logger)
    {
        Workflow = workflow;
        WorkflowState = workflowState;
        _serviceScopeFactory = serviceScopeFactory;
        _identityGenerator = identityGenerator;
        _logger = logger;
    }

    /// <inheritdoc />
    public Workflow Workflow { get; set; }

    /// <inheritdoc />
    public WorkflowState WorkflowState { get; set; }

    /// <inheritdoc />
    public async Task<bool> CanStartWorkflowAsync(StartWorkflowHostOptions? options = default, CancellationToken cancellationToken = default)
    {
        var strategyType = Workflow.Options.ActivationStrategyType;

        if (strategyType == null)
            return true;

        using var scope = _serviceScopeFactory.CreateScope();
        var instantiationStrategies = scope.ServiceProvider.GetServices<IWorkflowActivationStrategy>();
        var strategy = instantiationStrategies.FirstOrDefault(x => x.GetType() == strategyType);

        if (strategy == null)
            return true;

        var correlationId = options?.CorrelationId;
        var strategyContext = new WorkflowInstantiationStrategyContext(Workflow, correlationId, cancellationToken);
        return await strategy.GetAllowActivationAsync(strategyContext);
    }

    /// <inheritdoc />
    public async Task<StartWorkflowHostResult> StartWorkflowAsync(StartWorkflowHostOptions? options = default, CancellationToken cancellationToken = default)
    {
        var correlationId = options?.CorrelationId;
        var instanceId = options?.InstanceId ?? _identityGenerator.GenerateId();
        var originalBookmarks = WorkflowState.Bookmarks.ToList();
        var input = options?.Input;
        var properties = options?.Properties;

        var runOptions = new RunWorkflowOptions
        {
            WorkflowInstanceId = instanceId,
            CorrelationId = correlationId,
            Input = input,
            Properties = properties,
            TriggerActivityId = options?.TriggerActivityId,
            CancellationTokens = options?.CancellationTokens ?? cancellationToken
        };

        using var scope = _serviceScopeFactory.CreateScope();
        var workflowRunner = scope.ServiceProvider.GetRequiredService<IWorkflowRunner>();
        var workflowResult = await workflowRunner.RunAsync(Workflow, runOptions, cancellationToken);

        WorkflowState = workflowResult.WorkflowState;

        var updatedBookmarks = WorkflowState.Bookmarks;
        var diff = Diff.For(originalBookmarks, updatedBookmarks);

        return new StartWorkflowHostResult(diff);
    }

    /// <summary>
    /// Resumes the <see cref="Workflow"/>.
    /// </summary>
    public async Task<ResumeWorkflowHostResult> ResumeWorkflowAsync(ResumeWorkflowHostOptions? options = default, CancellationToken cancellationToken = default)
    {
        var originalBookmarks = WorkflowState.Bookmarks.ToList();

        if (WorkflowState.Status != WorkflowStatus.Running)
        {
            _logger.LogWarning("Attempt to resume workflow {WorkflowInstanceId} that is not in the Running state. The actual state is {ActualWorkflowStatus}", WorkflowState.Id, WorkflowState.Status);
            return new ResumeWorkflowHostResult(Diff.For(originalBookmarks, new List<Bookmark>()));
        }

        var instanceId = WorkflowState.Id;
        var input = options?.Input;

        var runOptions = new RunWorkflowOptions
        {
            WorkflowInstanceId = instanceId,
            CorrelationId = options?.CorrelationId,
            BookmarkId = options?.BookmarkId,
            ActivityId = options?.ActivityId,
            ActivityNodeId = options?.ActivityNodeId,
            ActivityInstanceId = options?.ActivityInstanceId,
            ActivityHash = options?.ActivityHash,
            Input = input,
            Properties = options?.Properties,
            CancellationTokens = options?.CancellationTokens ?? cancellationToken
        };

        using var scope = _serviceScopeFactory.CreateScope();
        var workflowRunner = scope.ServiceProvider.GetRequiredService<IWorkflowRunner>();
        var workflowResult = await workflowRunner.RunAsync(Workflow, WorkflowState, runOptions, cancellationToken);

        WorkflowState = workflowResult.WorkflowState;

        var updatedBookmarks = WorkflowState.Bookmarks;
        return new ResumeWorkflowHostResult(Diff.For(originalBookmarks, updatedBookmarks));
    }
}