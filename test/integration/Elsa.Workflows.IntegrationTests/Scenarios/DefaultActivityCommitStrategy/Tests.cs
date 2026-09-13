using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.CommitStates;
using Elsa.Workflows.CommitStates.Strategies;
using Elsa.Workflows.CommitStates.Tasks;
using Elsa.Workflows.IntegrationTests.SharedHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.IntegrationTests.Scenarios.DefaultActivityCommitStrategy;

public class Tests
{
    private readonly TextWriter _testOutput;

    public Tests()
    {
        _testOutput = TestContext.Current!.Output.StandardOutput;
    }

    [Test]
    [DisplayName("Activity without explicit strategy uses default commit strategy")]
    public async Task ActivityUsesDefaultCommitStrategy()
    {
        // Arrange
        var commitTracker = new CommitTracker();
        var defaultStrategy = new ExecutedActivityStrategy();
        await using var services = (ServiceProvider)new TestApplicationBuilder(_testOutput)
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
        await Assert.That(result.WorkflowState.Status).IsEqualTo(WorkflowStatus.Finished);
        await Assert.That(options.Value.DefaultActivityCommitStrategy).IsNotNull();
        await Assert.That(options.Value.DefaultActivityCommitStrategy).IsSameReferenceAs(defaultStrategy);

        // 6 commits: 3 WriteLine activities + 3 Sequence (composite) completion checks
        await Assert.That(commitTracker.CommitCount).IsEqualTo(6);
    }

    [Test]
    [DisplayName("Activity-specific strategy overrides default commit strategy")]
    public async Task ActivitySpecificStrategyOverridesDefault()
    {
        // Arrange
        var commitTracker = new CommitTracker();
        var defaultStrategy = new ExecutedActivityStrategy();
        await using var services = (ServiceProvider)new TestApplicationBuilder(_testOutput)
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
        await Assert.That(result.WorkflowState.Status).IsEqualTo(WorkflowStatus.Finished);
        await Assert.That(options.Value.DefaultActivityCommitStrategy).IsNotNull();
        await Assert.That(options.Value.DefaultActivityCommitStrategy).IsSameReferenceAs(defaultStrategy);

        // 4 commits: First activity with ExecutingActivity (before), second with default ExecutedActivity (after),
        // plus Sequence composite completions
        await Assert.That(commitTracker.CommitCount).IsEqualTo(4);
    }

    [Test]
    [DisplayName("No commits occur when no default strategy and no activity strategy")]
    public async Task NoCommitsWithoutAnyStrategy()
    {
        // Arrange
        var commitTracker = new CommitTracker();
        await using var services = (ServiceProvider)new TestApplicationBuilder(_testOutput)
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
        await Assert.That(result.WorkflowState.Status).IsEqualTo(WorkflowStatus.Finished);
        await Assert.That(options.Value.DefaultActivityCommitStrategy).IsNull();

        // 1 commit: Only the final commit from WorkflowRunner (no middleware commits)
        await Assert.That(commitTracker.CommitCount).IsEqualTo(1);
    }

    [Test]
    [DisplayName("Default activity strategy is not visible in commit strategy registry")]
    public async Task DefaultStrategyNotInRegistry()
    {
        // Arrange
        var defaultStrategy = new ExecutedActivityStrategy();
        await using var services = (ServiceProvider)new TestApplicationBuilder(_testOutput)
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
        await Assert.That(activityStrategies).IsEmpty();
        await Assert.That(options.Value.DefaultActivityCommitStrategy).IsNotNull();
        await Assert.That(options.Value.DefaultActivityCommitStrategy).IsSameReferenceAs(defaultStrategy);
    }

    [Test]
    [DisplayName("Default activity strategy with standard strategies does not duplicate")]
    public async Task DefaultStrategyWithStandardStrategiesNoDuplicate()
    {
        // Arrange
        var defaultStrategy = new ExecutedActivityStrategy();
        await using var services = (ServiceProvider)new TestApplicationBuilder(_testOutput)
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
        await Assert.That(activityStrategies.Count).IsEqualTo(4);
    }

    [Test]
    [DisplayName("Default workflow strategy is used when no default activity strategy is specified")]
    public async Task DefaultWorkflowStrategyWithoutDefaultActivityStrategy()
    {
        // Arrange
        var commitTracker = new CommitTracker();
        var defaultWorkflowStrategy = new ActivityExecutedWorkflowStrategy();
        await using var services = (ServiceProvider)new TestApplicationBuilder(_testOutput)
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
        await Assert.That(result.WorkflowState.Status).IsEqualTo(WorkflowStatus.Finished);
        await Assert.That(options.Value.DefaultWorkflowCommitStrategy).IsNotNull();
        await Assert.That(options.Value.DefaultWorkflowCommitStrategy).IsSameReferenceAs(defaultWorkflowStrategy);
        await Assert.That(options.Value.DefaultActivityCommitStrategy).IsNull();

        // 6 commits: ActivityExecutedWorkflowStrategy commits after each activity completion (3 WriteLine + 3 Sequence)
        await Assert.That(commitTracker.CommitCount).IsEqualTo(6);
    }
}
