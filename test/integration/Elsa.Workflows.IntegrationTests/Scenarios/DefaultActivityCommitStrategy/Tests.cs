using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.CommitStates;
using Elsa.Workflows.CommitStates.Strategies;
using Elsa.Workflows.CommitStates.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Scenarios.DefaultActivityCommitStrategy;

public class Tests(ITestOutputHelper testOutputHelper)
{
    private readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

    [Fact(DisplayName = "Activity without explicit strategy uses default commit strategy")]
    public async Task ActivityUsesDefaultCommitStrategy()
    {
        // Arrange
        var commitTracker = new CommitTracker();
        var defaultStrategy = new ExecutedActivityStrategy();
        var services = new TestApplicationBuilder(_testOutputHelper)
            .ConfigureElsa(elsa => elsa
                .UseWorkflows(workflows =>
                {
                    workflows.WithDefaultActivityCommitStrategy(defaultStrategy);
                    workflows.CommitStateHandler = _ => commitTracker;
                })
            )
            .AddWorkflow<SimpleWorkflowWithoutActivityCommitStrategy>()
            .Build();

        var options = services.GetRequiredService<IOptions<CommitStateOptions>>();
        var workflowRunner = services.GetRequiredService<IWorkflowRunner>();

        // Act
        var result = await workflowRunner.RunAsync<SimpleWorkflowWithoutActivityCommitStrategy>();

        // Assert
        Assert.Equal(WorkflowStatus.Finished, result.WorkflowState.Status);
        Assert.NotNull(options.Value.DefaultActivityCommitStrategy);
        Assert.Same(defaultStrategy, options.Value.DefaultActivityCommitStrategy);

        // 6 commits: 3 WriteLine activities + 3 Sequence (composite) completion checks
        Assert.Equal(6, commitTracker.CommitCount);
    }

    [Fact(DisplayName = "Activity-specific strategy overrides default commit strategy")]
    public async Task ActivitySpecificStrategyOverridesDefault()
    {
        // Arrange
        var commitTracker = new CommitTracker();
        var defaultStrategy = new ExecutedActivityStrategy();
        var services = new TestApplicationBuilder(_testOutputHelper)
            .ConfigureElsa(elsa => elsa
                .UseWorkflows(workflows =>
                {
                    workflows.WithDefaultActivityCommitStrategy(defaultStrategy);
                    workflows.UseCommitStrategies(commitStrategies => commitStrategies.AddStandardStrategies());
                    workflows.CommitStateHandler = _ => commitTracker;
                })
            )
            .AddWorkflow<WorkflowWithExplicitActivityCommitStrategy>()
            .Build();

        var options = services.GetRequiredService<IOptions<CommitStateOptions>>();
        var workflowRunner = services.GetRequiredService<IWorkflowRunner>();

        // Act
        var result = await workflowRunner.RunAsync<WorkflowWithExplicitActivityCommitStrategy>();

        // Assert
        Assert.Equal(WorkflowStatus.Finished, result.WorkflowState.Status);
        Assert.NotNull(options.Value.DefaultActivityCommitStrategy);
        Assert.Same(defaultStrategy, options.Value.DefaultActivityCommitStrategy);

        // 4 commits: First activity with ExecutingActivity (before), second with default ExecutedActivity (after),
        // plus Sequence composite completions
        Assert.Equal(4, commitTracker.CommitCount);
    }

    [Fact(DisplayName = "No commits occur when no default strategy and no activity strategy")]
    public async Task NoCommitsWithoutAnyStrategy()
    {
        // Arrange
        var commitTracker = new CommitTracker();
        var services = new TestApplicationBuilder(_testOutputHelper)
            .ConfigureElsa(elsa => elsa
                .UseWorkflows(workflows => workflows.CommitStateHandler = _ => commitTracker)
            )
            .AddWorkflow<SimpleWorkflowWithoutActivityCommitStrategy>()
            .Build();

        var options = services.GetRequiredService<IOptions<CommitStateOptions>>();
        var workflowRunner = services.GetRequiredService<IWorkflowRunner>();

        // Act
        var result = await workflowRunner.RunAsync<SimpleWorkflowWithoutActivityCommitStrategy>();

        // Assert
        Assert.Equal(WorkflowStatus.Finished, result.WorkflowState.Status);
        Assert.Null(options.Value.DefaultActivityCommitStrategy);

        // 1 commit: Only the final commit from WorkflowRunner (no middleware commits)
        Assert.Equal(1, commitTracker.CommitCount);
    }

    [Fact(DisplayName = "Default activity strategy is not visible in commit strategy registry")]
    public async Task DefaultStrategyNotInRegistry()
    {
        // Arrange
        var defaultStrategy = new ExecutedActivityStrategy();
        var services = new TestApplicationBuilder(_testOutputHelper)
            .ConfigureElsa(elsa => elsa
                .UseWorkflows(workflows => workflows
                    .WithDefaultActivityCommitStrategy(defaultStrategy)
                )
            )
            .Build();

        var registry = services.GetRequiredService<ICommitStrategyRegistry>();
        var options = services.GetRequiredService<IOptions<CommitStateOptions>>();

        // Act
        var activityStrategies = registry.ListActivityStrategyRegistrations().ToList();

        // Assert
        Assert.Empty(activityStrategies);
        Assert.NotNull(options.Value.DefaultActivityCommitStrategy);
        Assert.Same(defaultStrategy, options.Value.DefaultActivityCommitStrategy);
        await Task.CompletedTask;
    }

    [Fact(DisplayName = "Default activity strategy with standard strategies does not duplicate")]
    public async Task DefaultStrategyWithStandardStrategiesNoDuplicate()
    {
        // Arrange
        var defaultStrategy = new ExecutedActivityStrategy();
        var services = new TestApplicationBuilder(_testOutputHelper)
            .ConfigureElsa(elsa => elsa
                .UseWorkflows(workflows => workflows
                    .WithDefaultActivityCommitStrategy(defaultStrategy)
                    .UseCommitStrategies(commitStrategies => commitStrategies.AddStandardStrategies())
                )
            )
            .Build();

        var registry = services.GetRequiredService<ICommitStrategyRegistry>();

        // Manually populate the registry
        var startupTask = new PopulateCommitStrategyRegistry(
            registry,
            services.GetRequiredService<IOptions<CommitStateOptions>>()
        );
        await startupTask.ExecuteAsync(CancellationToken.None);

        // Act
        var activityStrategies = registry.ListActivityStrategyRegistrations().ToList();

        // Assert - 4 standard activity strategies (no duplication from default)
        Assert.Equal(4, activityStrategies.Count);
    }

    [Fact(DisplayName = "Default workflow strategy is used when no default activity strategy is specified")]
    public async Task DefaultWorkflowStrategyWithoutDefaultActivityStrategy()
    {
        // Arrange
        var commitTracker = new CommitTracker();
        var defaultWorkflowStrategy = new ActivityExecutedWorkflowStrategy();
        var services = new TestApplicationBuilder(_testOutputHelper)
            .ConfigureElsa(elsa => elsa
                .UseWorkflows(workflows =>
                {
                    workflows.WithDefaultWorkflowCommitStrategy(defaultWorkflowStrategy);
                    workflows.CommitStateHandler = _ => commitTracker;
                })
            )
            .AddWorkflow<SimpleWorkflowWithoutActivityCommitStrategy>()
            .Build();

        var options = services.GetRequiredService<IOptions<CommitStateOptions>>();
        var workflowRunner = services.GetRequiredService<IWorkflowRunner>();

        // Act
        var result = await workflowRunner.RunAsync<SimpleWorkflowWithoutActivityCommitStrategy>();

        // Assert
        Assert.Equal(WorkflowStatus.Finished, result.WorkflowState.Status);
        Assert.NotNull(options.Value.DefaultWorkflowCommitStrategy);
        Assert.Same(defaultWorkflowStrategy, options.Value.DefaultWorkflowCommitStrategy);
        Assert.Null(options.Value.DefaultActivityCommitStrategy);

        // 6 commits: ActivityExecutedWorkflowStrategy commits after each activity completion (3 WriteLine + 3 Sequence)
        Assert.Equal(6, commitTracker.CommitCount);
    }
}
