using Elsa.Common;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;
using Elsa.Workflows.Options;
using Elsa.Workflows.Services;
using Elsa.Workflows.State;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Elsa.Workflows.Core.UnitTests.Services;

public class WorkflowStateExtractorTests
{
    [Test]
    public async Task Extract_And_Apply_PreservesScheduledActivityMetadata()
    {
        var root = new WriteLine("root");
        var fixture = new ActivityTestFixture(root).ConfigureServices(services =>
        {
            services.RemoveAll<IWorkflowExecutionContextSchedulerStrategy>();
            services.AddSingleton<IWorkflowExecutionContextSchedulerStrategy, WorkflowExecutionContextSchedulerStrategy>();
        });
        var context = await fixture.BuildAsync();
        context.Id = "owner-context";
        context.WorkflowExecutionContext.AddActivityExecutionContext(context);
        var scheduledWorkItem = new ActivityWorkItem(
            root,
            context,
            tag: "scheduled",
            schedulingActivityExecutionId: "predecessor",
            schedulingWorkflowInstanceId: "parent-workflow",
            schedulingCallStackDepth: 3);
        context.WorkflowExecutionContext.Scheduler.Schedule(scheduledWorkItem);

        var sourceWorkflowContext = context.WorkflowExecutionContext;
        var extractor = sourceWorkflowContext.GetRequiredService<IWorkflowStateExtractor>();
        var state = extractor.Extract(sourceWorkflowContext);
        var resumedWorkflowContext = await WorkflowExecutionContext.CreateAsync(
            sourceWorkflowContext.ServiceProvider,
            sourceWorkflowContext.WorkflowGraph,
            state.Id,
            CancellationToken.None);

        await extractor.ApplyAsync(resumedWorkflowContext, state);

        var restoredWorkItem = await Assert.That(resumedWorkflowContext.Scheduler.List()).HasSingleItem();
        await Assert.That(restoredWorkItem.Owner?.Id).IsEqualTo("owner-context");
        await Assert.That(restoredWorkItem.Tag).IsEqualTo("scheduled");
        await Assert.That(restoredWorkItem.SchedulingActivityExecutionId).IsEqualTo("predecessor");
        await Assert.That(restoredWorkItem.SchedulingWorkflowInstanceId).IsEqualTo("parent-workflow");
        await Assert.That(restoredWorkItem.SchedulingCallStackDepth).IsEqualTo(3);
    }

    [Test]
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
        await Assert.That(restoredContextA).IsNotNull();
        await Assert.That(restoredContextA.CallStackDepth).IsEqualTo(10);
    }

    [Test]
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
        await Assert.That(context.CallStackDepth).IsEqualTo(schedulingDepth + 1);
        await Assert.That(context.SchedulingActivityExecutionId).IsEqualTo("parent-activity-id");
        await Assert.That(context.SchedulingWorkflowInstanceId).IsEqualTo("parent-workflow-id");
    }

    [Test]
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
        await Assert.That(childContext.CallStackDepth).IsEqualTo(4);
        await Assert.That(childContext.SchedulingActivityExecutionId).IsEqualTo(parentContext.Id);
    }

    [Test]
    [Arguments("1", 1, false, "Unexpected")]
    [Arguments("persisted-version-id", 7, true, "MigrationCompatible")]
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
        await Assert.That(testContext.TargetContext.ActivityExecutionContexts).IsEmpty();
        var warning = await Assert.That(testContext.Logger.Entries).HasSingleItem();
        await Assert.That(warning.Level).IsEqualTo(LogLevel.Warning);
        await Assert.That(warning.Properties["WorkflowStateSkipKind"]).IsEqualTo("ActivityExecutionContext");
        await Assert.That(warning.Properties["ActivityExecutionContextId"]).IsEqualTo("missing-activity-context");
        await Assert.That(warning.Properties["ScheduledActivityNodeId"]).IsEqualTo("missing-activity-node");
        await AssertDefinitionProperties(warning, state, testContext.TargetContext, isMigration, expectedClassification);
    }

    [Test]
    public async Task ApplyAsync_WhenCompletionCallbackOwnerIsMissing_LogsStructuredWarning()
    {
        // Arrange
        var testContext = await CreateTestContextAsync();
        var state = testContext.State;
        state.CompletionCallbacks.Add(new("missing-owner", "child-node", null));

        // Act
        await testContext.Extractor.ApplyAsync(testContext.TargetContext, state);

        // Assert
        await Assert.That(testContext.TargetContext.CompletionCallbacks).IsEmpty();
        var warning = await Assert.That(testContext.Logger.Entries).HasSingleItem();
        await Assert.That(warning.Level).IsEqualTo(LogLevel.Warning);
        await Assert.That(warning.Properties["WorkflowStateSkipKind"]).IsEqualTo("CompletionCallbackOwner");
        await Assert.That(warning.Properties["CompletionCallbackOwnerInstanceId"]).IsEqualTo("missing-owner");
        await Assert.That(warning.Properties["CompletionCallbackChildNodeId"]).IsEqualTo("child-node");
        await AssertDefinitionProperties(warning, state, testContext.TargetContext, false, "Unexpected");
    }

    [Test]
    public async Task ApplyAsync_WhenCompletionCallbackChildIsMissing_LogsStructuredWarning()
    {
        // Arrange
        var testContext = await CreateTestContextAsync(includeActivityExecutionContext: true);
        var state = testContext.State;
        var owner = await Assert.That(state.ActivityExecutionContexts).HasSingleItem();
        var ownerInstanceId = owner.Id;
        state.CompletionCallbacks.Add(new(ownerInstanceId, "missing-child-node", null));

        // Act
        await testContext.Extractor.ApplyAsync(testContext.TargetContext, state);

        // Assert
        await Assert.That(testContext.TargetContext.CompletionCallbacks).IsEmpty();
        var warning = await Assert.That(testContext.Logger.Entries).HasSingleItem();
        await Assert.That(warning.Level).IsEqualTo(LogLevel.Warning);
        await Assert.That(warning.Properties["WorkflowStateSkipKind"]).IsEqualTo("CompletionCallbackChild");
        await Assert.That(warning.Properties["CompletionCallbackOwnerInstanceId"]).IsEqualTo(ownerInstanceId);
        await Assert.That(warning.Properties["CompletionCallbackChildNodeId"]).IsEqualTo("missing-child-node");
        await AssertDefinitionProperties(warning, state, testContext.TargetContext, false, "Unexpected");
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

    private static async Task AssertDefinitionProperties(
        CapturedLogEntry warning,
        WorkflowState state,
        WorkflowExecutionContext targetContext,
        bool isMigration,
        string expectedClassification)
    {
        var targetIdentity = targetContext.Workflow.Identity;
        await Assert.That(warning.Properties["WorkflowInstanceId"]).IsEqualTo(state.Id);
        await Assert.That(warning.Properties["PersistedWorkflowDefinitionId"]).IsEqualTo(state.DefinitionId);
        await Assert.That(warning.Properties["PersistedWorkflowDefinitionVersionId"]).IsEqualTo(state.DefinitionVersionId);
        await Assert.That(warning.Properties["PersistedWorkflowDefinitionVersion"]).IsEqualTo(state.DefinitionVersion);
        await Assert.That(warning.Properties["TargetWorkflowDefinitionId"]).IsEqualTo(targetIdentity.DefinitionId);
        await Assert.That(warning.Properties["TargetWorkflowDefinitionVersionId"]).IsEqualTo(targetIdentity.Id);
        await Assert.That(warning.Properties["TargetWorkflowDefinitionVersion"]).IsEqualTo(targetIdentity.Version);
        await Assert.That(warning.Properties["IsWorkflowDefinitionVersionMigration"]).IsEqualTo(isMigration);
        await Assert.That(warning.Properties["WorkflowStateSkipClassification"]).IsEqualTo(expectedClassification);
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
