using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.CommitStates;
using Elsa.Workflows.CommitStates.Strategies;
using Elsa.Workflows.IntegrationTests.SharedHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.IntegrationTests.Scenarios.DefaultWorkflowCommitStrategy;

public class Tests
{
    private readonly TextWriter _testOutput;

    public Tests()
    {
        _testOutput = TestContext.Current!.Output.StandardOutput;
    }

    [Test]
    [DisplayName("Workflow uses default workflow commit strategy when no explicit workflow commit strategy is set")]
    public async Task WorkflowUsesDefaultWorkflowCommitStrategy()
    {
        // Arrange
        var commitTracker = new CommitTracker();
        var defaultStrategy = new ActivityExecutedWorkflowStrategy();
        await using var services = (ServiceProvider)new TestApplicationBuilder(_testOutput)
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
        await Assert.That(result.WorkflowState.Status).IsEqualTo(WorkflowStatus.Finished);
        await Assert.That(options.Value.DefaultWorkflowCommitStrategy).IsNotNull();
        await Assert.That(options.Value.DefaultWorkflowCommitStrategy).IsSameReferenceAs(defaultStrategy);

        // Verify exact commit count: ActivityExecutedWorkflowStrategy commits after each activity completes
        // With 3 WriteLine activities in a Sequence, this results in exactly 6 commits due to
        // how composite activities and workflow completion signals interact
        await Assert.That(commitTracker.CommitCount).IsEqualTo(6);
    }

    [Test]
    [DisplayName("Workflow-specific strategy overrides default workflow commit strategy")]
    public async Task WorkflowSpecificStrategyOverridesDefault()
    {
        // Arrange
        var commitTracker = new CommitTracker();
        var defaultStrategy = new ActivityExecutedWorkflowStrategy();
        await using var services = (ServiceProvider)new TestApplicationBuilder(_testOutput)
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
        await Assert.That(result.WorkflowState.Status).IsEqualTo(WorkflowStatus.Finished);
        // Default strategy should be configured (available for workflows without explicit strategy)
        await Assert.That(options.Value.DefaultWorkflowCommitStrategy).IsSameReferenceAs(defaultStrategy);

        // Verify the workflow used its explicit "WorkflowExecuting" strategy (commits before workflow starts)
        // 1 commit at the beginning before any activities execute
        await Assert.That(commitTracker.CommitCount).IsEqualTo(1);
    }

    [Test]
    [DisplayName("No commits occur when no default workflow commit strategy and no workflow strategy")]
    public async Task NoCommitsWithoutAnyWorkflowCommitStrategy()
    {
        // Arrange
        var commitTracker = new CommitTracker();
        await using var services = (ServiceProvider)new TestApplicationBuilder(_testOutput)
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
        await Assert.That(result.WorkflowState.Status).IsEqualTo(WorkflowStatus.Finished);
        // No default strategy should be configured
        await Assert.That(options.Value.DefaultWorkflowCommitStrategy).IsNull();

        // Verify no commits occurred during workflow execution (only final commit from WorkflowRunner)
        await Assert.That(commitTracker.CommitCount).IsEqualTo(1);
    }

    [Test]
    [DisplayName("Default workflow strategy is not visible in commit strategy registry")]
    public async Task DefaultWorkflowStrategyNotInRegistry()
    {
        // Arrange
        await using var services = (ServiceProvider)new TestApplicationBuilder(_testOutput)
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
        await Assert.That(registeredStrategies).IsEmpty();
    }

    [Test]
    [DisplayName("Default workflow strategy with standard strategies does not duplicate")]
    public async Task DefaultWorkflowStrategyWithStandardStrategiesNoDuplicate()
    {
        // Arrange
        await using var services = (ServiceProvider)new TestApplicationBuilder(_testOutput)
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
        await Assert.That(registeredStrategies.Count).IsEqualTo(4);
    }
}
