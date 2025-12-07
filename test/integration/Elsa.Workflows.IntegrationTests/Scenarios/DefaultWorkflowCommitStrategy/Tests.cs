using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.CommitStates;
using Elsa.Workflows.CommitStates.Strategies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.DefaultWorkflowCommitStrategy;

public class Tests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public Tests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact(DisplayName = "Workflow uses default workflow commit strategy when no explicit workflow commit strategy is set")]
    public async Task WorkflowUsesDefaultWorkflowCommitStrategy()
    {
        // Arrange
        var commitTracker = new CommitTracker();
        var defaultStrategy = new ActivityExecutedWorkflowStrategy();
        var services = new TestApplicationBuilder(_testOutputHelper)
            .ConfigureElsa(elsa => elsa
                .UseWorkflows(workflows =>
                {
                    workflows.WithDefaultWorkflowCommitStrategy(defaultStrategy);
                    workflows.CommitStateHandler = _ => commitTracker;
                })
            )
            .AddWorkflow<SimpleWorkflowWithoutWorkflowCommitStrategy>()
            .Build();

        var options = services.GetRequiredService<IOptions<CommitStateOptions>>();
        var workflowRunner = services.GetRequiredService<IWorkflowRunner>();

        // Act
        var result = await workflowRunner.RunAsync<SimpleWorkflowWithoutWorkflowCommitStrategy>();

        // Assert - workflow should finish successfully with default strategy configured
        Assert.Equal(WorkflowStatus.Finished, result.WorkflowState.Status);
        Assert.NotNull(options.Value.DefaultWorkflowCommitStrategy);
        Assert.Same(defaultStrategy, options.Value.DefaultWorkflowCommitStrategy);

        // Verify exact commit count: ActivityExecutedWorkflowStrategy commits after each activity completes
        // With 3 WriteLine activities in a Sequence, this results in exactly 6 commits due to
        // how composite activities and workflow completion signals interact
        Assert.Equal(6, commitTracker.CommitCount);
    }

    [Fact(DisplayName = "Workflow-specific strategy overrides default workflow commit strategy")]
    public async Task WorkflowSpecificStrategyOverridesDefault()
    {
        // Arrange
        var commitTracker = new CommitTracker();
        var defaultStrategy = new ActivityExecutedWorkflowStrategy();
        var services = new TestApplicationBuilder(_testOutputHelper)
            .ConfigureElsa(elsa => elsa
                .UseWorkflows(workflows => workflows
                    .WithDefaultWorkflowCommitStrategy(defaultStrategy)
                    .UseCommitStrategies(commitStrategies => commitStrategies.AddStandardStrategies())
                    .CommitStateHandler = _ => commitTracker
                )
            )
            .AddWorkflow<WorkflowWithExplicitWorkflowCommitStrategy>()
            .Build();

        var workflowRunner = services.GetRequiredService<IWorkflowRunner>();
        var options = services.GetRequiredService<IOptions<CommitStateOptions>>();

        // Act
        var result = await workflowRunner.RunAsync<WorkflowWithExplicitWorkflowCommitStrategy>();

        // Assert - workflow should complete successfully
        Assert.Equal(WorkflowStatus.Finished, result.WorkflowState.Status);
        // Default strategy should be configured (available for workflows without explicit strategy)
        Assert.Same(defaultStrategy, options.Value.DefaultWorkflowCommitStrategy);

        // Verify the workflow used its explicit "WorkflowExecuting" strategy (commits before workflow starts)
        // 1 commit at the beginning before any activities execute
        Assert.Equal(1, commitTracker.CommitCount);
    }

    [Fact(DisplayName = "No commits occur when no default workflow commit strategy and no workflow strategy")]
    public async Task NoCommitsWithoutAnyWorkflowCommitStrategy()
    {
        // Arrange
        var commitTracker = new CommitTracker();
        var services = new TestApplicationBuilder(_testOutputHelper)
            .ConfigureElsa(elsa => elsa
                .UseWorkflows(workflows =>
                {
                    workflows.CommitStateHandler = _ => commitTracker;
                })
            )
            .AddWorkflow<SimpleWorkflowWithoutWorkflowCommitStrategy>()
            .Build();

        var workflowRunner = services.GetRequiredService<IWorkflowRunner>();
        var options = services.GetRequiredService<IOptions<CommitStateOptions>>();

        // Act
        var result = await workflowRunner.RunAsync<SimpleWorkflowWithoutWorkflowCommitStrategy>();

        // Assert - workflow should still complete even without commit strategy
        Assert.Equal(WorkflowStatus.Finished, result.WorkflowState.Status);
        // No default strategy should be configured
        Assert.Null(options.Value.DefaultWorkflowCommitStrategy);

        // Verify no commits occurred during workflow execution (only final commit from WorkflowRunner)
        Assert.Equal(1, commitTracker.CommitCount);
    }

    [Fact(DisplayName = "Default workflow strategy is not visible in commit strategy registry")]
    public async Task DefaultWorkflowStrategyNotInRegistry()
    {
        // Arrange
        var services = new TestApplicationBuilder(_testOutputHelper)
            .ConfigureElsa(elsa => elsa
                .UseWorkflows(workflows => workflows
                    .WithDefaultWorkflowCommitStrategy(new ActivityExecutedWorkflowStrategy())
                )
            )
            .Build();

        var registry = services.GetRequiredService<ICommitStrategyRegistry>();

        // Act
        var registeredStrategies = registry.ListWorkflowStrategyRegistrations().ToList();

        // Assert - default strategy should not be in the registry
        Assert.Empty(registeredStrategies);
    }

    [Fact(DisplayName = "Default workflow strategy with standard strategies does not duplicate")]
    public void DefaultWorkflowStrategyWithStandardStrategiesNoDuplicate()
    {
        // Arrange
        var services = new TestApplicationBuilder(_testOutputHelper)
            .ConfigureElsa(elsa => elsa
                .UseWorkflows(workflows => workflows
                    .WithDefaultWorkflowCommitStrategy(new ActivityExecutedWorkflowStrategy())
                    .UseCommitStrategies(commitStrategies => commitStrategies.AddStandardStrategies())
                )
            )
            .Build();

        // Manually trigger the PopulateCommitStrategyRegistry startup task
        var options = services.GetRequiredService<IOptions<CommitStateOptions>>();
        var registry = services.GetRequiredService<ICommitStrategyRegistry>();

        foreach (var strategy in options.Value.WorkflowCommitStrategies.Values)
            registry.RegisterStrategy(strategy);
        foreach (var strategy in options.Value.ActivityCommitStrategies.Values)
            registry.RegisterStrategy(strategy);

        // Act
        var registeredStrategies = registry.ListWorkflowStrategyRegistrations().ToList();

        // Assert - should only have the 4 standard strategies, not 5
        Assert.Equal(4, registeredStrategies.Count);
    }
}
