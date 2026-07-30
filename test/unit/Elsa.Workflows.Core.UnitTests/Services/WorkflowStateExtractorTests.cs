using Elsa.Common;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Options;
using Elsa.Workflows.Services;
using Elsa.Workflows.State;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Elsa.Workflows.Core.UnitTests.Services;

public class WorkflowStateExtractorTests
{
    [Fact]
    public async Task Extract_And_Apply_PreservesCallStackDepth()
    {
        // Arrange
        var root = new WriteLine("root");
        var fixture = new ActivityTestFixture(root);
        var contextRoot = await fixture.BuildAsync();
        var workflowExecutionContext = contextRoot.WorkflowExecutionContext;
        
        var contextA = await workflowExecutionContext.CreateActivityExecutionContextAsync(root);
        contextA.CallStackDepth = 10; // Manually set for testing persistence
        workflowExecutionContext.AddActivityExecutionContext(contextA);

        var extractor = workflowExecutionContext.GetRequiredService<IWorkflowStateExtractor>();

        // Act
        var state = extractor.Extract(workflowExecutionContext);
        
        // Create a new context to apply the state to
        var newWorkflowExecutionContext = await WorkflowExecutionContext.CreateAsync(
            workflowExecutionContext.ServiceProvider,
            workflowExecutionContext.WorkflowGraph,
            state.Id,
            CancellationToken.None
        );
        
        await extractor.ApplyAsync(newWorkflowExecutionContext, state);

        // Assert
        var restoredContextA = newWorkflowExecutionContext.ActivityExecutionContexts.FirstOrDefault(x => x.Id == contextA.Id);
        Assert.NotNull(restoredContextA);
        Assert.Equal(10, restoredContextA.CallStackDepth);
    }

    [Fact]
    public async Task CallStackDepth_IsIncrementedWhenSchedulingCallStackDepthProvided()
    {
        // Arrange
        var root = new WriteLine("root");
        var fixture = new ActivityTestFixture(root);
        var contextRoot = await fixture.BuildAsync();
        var workflowExecutionContext = contextRoot.WorkflowExecutionContext;

        var schedulingDepth = 5;
        var options = new ActivityInvocationOptions
        {
            SchedulingActivityExecutionId = "parent-activity-id",
            SchedulingWorkflowInstanceId = "parent-workflow-id",
            SchedulingCallStackDepth = schedulingDepth
        };

        // Act - Create a new activity context with scheduling information
        var context = await workflowExecutionContext.CreateActivityExecutionContextAsync(root, options);

        // Assert - The CallStackDepth should be incremented from the scheduling depth
        Assert.Equal(schedulingDepth + 1, context.CallStackDepth);
        Assert.Equal("parent-activity-id", context.SchedulingActivityExecutionId);
        Assert.Equal("parent-workflow-id", context.SchedulingWorkflowInstanceId);
    }

    [Fact]
    public async Task CallStackDepth_IsIncrementedFromParentContext()
    {
        // Arrange
        var root = new WriteLine("root");
        var fixture = new ActivityTestFixture(root);
        var contextRoot = await fixture.BuildAsync();
        var workflowExecutionContext = contextRoot.WorkflowExecutionContext;

        // Create a parent context with depth 3
        var parentContext = await workflowExecutionContext.CreateActivityExecutionContextAsync(root);
        parentContext.CallStackDepth = 3;
        workflowExecutionContext.AddActivityExecutionContext(parentContext);

        var options = new ActivityInvocationOptions
        {
            SchedulingActivityExecutionId = parentContext.Id
        };

        // Act - Create a child context that references the parent
        var childContext = await workflowExecutionContext.CreateActivityExecutionContextAsync(root, options);

        // Assert - The CallStackDepth should be parent depth + 1
        Assert.Equal(4, childContext.CallStackDepth);
        Assert.Equal(parentContext.Id, childContext.SchedulingActivityExecutionId);
    }

    [Theory]
    [InlineData("1", 1, false, "Unexpected")]
    [InlineData("persisted-version-id", 7, true, "MigrationCompatible")]
    public async Task ApplyAsync_WhenActivityContextNodeIsMissing_LogsStructuredWarningAndSkipsContext(
        string persistedDefinitionVersionId,
        int persistedDefinitionVersion,
        bool isMigration,
        string expectedClassification)
    {
        // Arrange
        var testContext = await CreateTestContextAsync();
        var state = testContext.State;
        state.DefinitionVersionId = persistedDefinitionVersionId;
        state.DefinitionVersion = persistedDefinitionVersion;
        state.ActivityExecutionContexts.Add(new()
        {
            Id = "missing-activity-context",
            ScheduledActivityNodeId = "missing-activity-node"
        });

        // Act
        await testContext.Extractor.ApplyAsync(testContext.TargetContext, state);

        // Assert
        Assert.Empty(testContext.TargetContext.ActivityExecutionContexts);
        var warning = Assert.Single(testContext.Logger.Entries);
        Assert.Equal(LogLevel.Warning, warning.Level);
        Assert.Equal("ActivityExecutionContext", warning.Properties["WorkflowStateSkipKind"]);
        Assert.Equal("missing-activity-context", warning.Properties["ActivityExecutionContextId"]);
        Assert.Equal("missing-activity-node", warning.Properties["ScheduledActivityNodeId"]);
        AssertDefinitionProperties(warning, state, testContext.TargetContext, isMigration, expectedClassification);
    }

    [Fact]
    public async Task ApplyAsync_WhenCompletionCallbackOwnerIsMissing_LogsStructuredWarning()
    {
        // Arrange
        var testContext = await CreateTestContextAsync();
        var state = testContext.State;
        state.CompletionCallbacks.Add(new("missing-owner", "child-node", null));

        // Act
        await testContext.Extractor.ApplyAsync(testContext.TargetContext, state);

        // Assert
        Assert.Empty(testContext.TargetContext.CompletionCallbacks);
        var warning = Assert.Single(testContext.Logger.Entries);
        Assert.Equal(LogLevel.Warning, warning.Level);
        Assert.Equal("CompletionCallbackOwner", warning.Properties["WorkflowStateSkipKind"]);
        Assert.Equal("missing-owner", warning.Properties["CompletionCallbackOwnerInstanceId"]);
        Assert.Equal("child-node", warning.Properties["CompletionCallbackChildNodeId"]);
        AssertDefinitionProperties(warning, state, testContext.TargetContext, false, "Unexpected");
    }

    [Fact]
    public async Task ApplyAsync_WhenCompletionCallbackChildIsMissing_LogsStructuredWarning()
    {
        // Arrange
        var testContext = await CreateTestContextAsync(includeActivityExecutionContext: true);
        var state = testContext.State;
        var ownerInstanceId = Assert.Single(state.ActivityExecutionContexts).Id;
        state.CompletionCallbacks.Add(new(ownerInstanceId, "missing-child-node", null));

        // Act
        await testContext.Extractor.ApplyAsync(testContext.TargetContext, state);

        // Assert
        Assert.Empty(testContext.TargetContext.CompletionCallbacks);
        var warning = Assert.Single(testContext.Logger.Entries);
        Assert.Equal(LogLevel.Warning, warning.Level);
        Assert.Equal("CompletionCallbackChild", warning.Properties["WorkflowStateSkipKind"]);
        Assert.Equal(ownerInstanceId, warning.Properties["CompletionCallbackOwnerInstanceId"]);
        Assert.Equal("missing-child-node", warning.Properties["CompletionCallbackChildNodeId"]);
        AssertDefinitionProperties(warning, state, testContext.TargetContext, false, "Unexpected");
    }

    private static async Task<TestContext> CreateTestContextAsync(bool includeActivityExecutionContext = false)
    {
        var fixture = new ActivityTestFixture(new WriteLine("root"));
        var contextRoot = await fixture.BuildAsync();
        var sourceContext = contextRoot.WorkflowExecutionContext;

        if (includeActivityExecutionContext)
            sourceContext.AddActivityExecutionContext(contextRoot);

        var logger = new CapturingLogger<WorkflowStateExtractor>();
        var extractor = new WorkflowStateExtractor(logger);
        var state = extractor.Extract(sourceContext);
        var targetContext = await WorkflowExecutionContext.CreateAsync(
            sourceContext.ServiceProvider,
            sourceContext.WorkflowGraph,
            state.Id,
            CancellationToken.None);

        return new(extractor, logger, state, targetContext);
    }

    private static void AssertDefinitionProperties(
        CapturedLogEntry warning,
        WorkflowState state,
        WorkflowExecutionContext targetContext,
        bool isMigration,
        string expectedClassification)
    {
        var targetIdentity = targetContext.Workflow.Identity;
        Assert.Equal(state.Id, warning.Properties["WorkflowInstanceId"]);
        Assert.Equal(state.DefinitionId, warning.Properties["PersistedWorkflowDefinitionId"]);
        Assert.Equal(state.DefinitionVersionId, warning.Properties["PersistedWorkflowDefinitionVersionId"]);
        Assert.Equal(state.DefinitionVersion, warning.Properties["PersistedWorkflowDefinitionVersion"]);
        Assert.Equal(targetIdentity.DefinitionId, warning.Properties["TargetWorkflowDefinitionId"]);
        Assert.Equal(targetIdentity.Id, warning.Properties["TargetWorkflowDefinitionVersionId"]);
        Assert.Equal(targetIdentity.Version, warning.Properties["TargetWorkflowDefinitionVersion"]);
        Assert.Equal(isMigration, warning.Properties["IsWorkflowDefinitionVersionMigration"]);
        Assert.Equal(expectedClassification, warning.Properties["WorkflowStateSkipClassification"]);
    }

    private sealed record TestContext(
        WorkflowStateExtractor Extractor,
        CapturingLogger<WorkflowStateExtractor> Logger,
        WorkflowState State,
        WorkflowExecutionContext TargetContext);

    private sealed record CapturedLogEntry(LogLevel Level, string Message, IReadOnlyDictionary<string, object?> Properties);

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public ICollection<CapturedLogEntry> Entries { get; } = new List<CapturedLogEntry>();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var properties = state is IEnumerable<KeyValuePair<string, object?>> values
                ? values.ToDictionary(x => x.Key, x => x.Value)
                : new Dictionary<string, object?>();

            Entries.Add(new(logLevel, formatter(state, exception), properties));
        }

        private sealed class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new();

            public void Dispose()
            {
            }
        }
    }
}
