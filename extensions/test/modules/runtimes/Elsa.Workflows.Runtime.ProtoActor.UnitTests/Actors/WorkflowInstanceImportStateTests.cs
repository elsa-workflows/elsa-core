using Elsa.Workflows.Models;
using Elsa.Workflows.Options;
using Elsa.Workflows.Runtime.ProtoActor.UnitTests.Fixtures;
using Elsa.Workflows.State;
using Moq;
using Proto.Cluster;
using ProtoRunWorkflowInstanceResponse = Elsa.Workflows.Runtime.ProtoActor.ProtoBuf.RunWorkflowInstanceResponse;

namespace Elsa.Workflows.Runtime.ProtoActor.UnitTests.Actors;

public class WorkflowInstanceImportStateTests
{
    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task ImportState_WhenAlterationMigratesDefinition_RefreshesWorkflowGraph()
    {
        await using var harness = new WorkflowInstanceTestHarness();
        using var timeout = new CancellationTokenSource(TestTimeout);
        var initialState = WorkflowInstanceTestHarness.CreateState();
        var migratedState = WorkflowInstanceTestHarness.CreateState("definition-version-2", 2);
        var migratedGraph = WorkflowInstanceTestHarness.CreateWorkflowGraph(migratedState.DefinitionVersionId, migratedState.DefinitionVersion);
        harness.SetupExistingInstance(initialState);
        harness.WorkflowDefinitionService
            .Setup(x => x.FindWorkflowGraphAsync(
                It.Is<WorkflowDefinitionHandle>(handle => handle.DefinitionVersionId == migratedState.DefinitionVersionId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(migratedGraph);

        // Alterations import the migrated state through IWorkflowClient before the next run.
        Assert.IsType<GrainResponseMessage>(await harness.ImportStateAsync(migratedState, timeout.Token));
        harness.WorkflowInstanceManager.Verify(x => x.SaveAsync(migratedState, It.IsAny<CancellationToken>()), Times.Once);
        Assert.IsType<ProtoRunWorkflowInstanceResponse>(await harness.RunAsync(timeout.Token));

        harness.WorkflowRunner.Verify(x => x.RunAsync(
            migratedGraph, migratedState, It.IsAny<RunWorkflowOptions?>(), It.IsAny<CancellationToken>()), Times.Once);
        harness.WorkflowDefinitionService.Verify(x => x.FindWorkflowGraphAsync(
            It.Is<WorkflowDefinitionHandle>(handle => handle.DefinitionVersionId == WorkflowInstanceTestHarness.DefinitionVersionId),
            It.IsAny<CancellationToken>()), Times.Once);
        harness.WorkflowDefinitionService.Verify(x => x.FindWorkflowGraphAsync(
            It.Is<WorkflowDefinitionHandle>(handle => handle.DefinitionVersionId == migratedState.DefinitionVersionId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ImportState_WhenDefinitionVersionIsUnchanged_ReusesCachedWorkflowGraph()
    {
        await using var harness = new WorkflowInstanceTestHarness();
        using var timeout = new CancellationTokenSource(TestTimeout);
        var initialState = WorkflowInstanceTestHarness.CreateState();
        var importedState = WorkflowInstanceTestHarness.CreateState();
        importedState.Properties["alteration"] = "same-version";
        harness.SetupExistingInstance(initialState);

        Assert.IsType<GrainResponseMessage>(await harness.ImportStateAsync(importedState, timeout.Token));
        harness.WorkflowInstanceManager.Verify(x => x.SaveAsync(importedState, It.IsAny<CancellationToken>()), Times.Once);
        Assert.IsType<ProtoRunWorkflowInstanceResponse>(await harness.RunAsync(timeout.Token));

        harness.WorkflowRunner.Verify(x => x.RunAsync(
            harness.WorkflowGraph, importedState, It.IsAny<RunWorkflowOptions?>(), It.IsAny<CancellationToken>()), Times.Once);
        harness.WorkflowDefinitionService.Verify(x => x.FindWorkflowGraphAsync(
            It.IsAny<WorkflowDefinitionHandle>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ImportState_WhenImportedDefinitionGraphIsMissing_PreservesCachedPairAndDoesNotSave()
    {
        await using var harness = new WorkflowInstanceTestHarness();
        using var timeout = new CancellationTokenSource(TestTimeout);
        var initialState = WorkflowInstanceTestHarness.CreateState();
        var migratedState = WorkflowInstanceTestHarness.CreateState("missing-definition-version", 2);
        harness.SetupExistingInstance(initialState);
        harness.WorkflowDefinitionService
            .Setup(x => x.FindWorkflowGraphAsync(
                It.Is<WorkflowDefinitionHandle>(handle => handle.DefinitionVersionId == migratedState.DefinitionVersionId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkflowGraph?)null);

        var error = Assert.IsType<GrainErrorResponse>(await harness.ImportStateAsync(migratedState, timeout.Token));
        Assert.Contains("missing-definition-version", error.Err);
        harness.WorkflowInstanceManager.Verify(x => x.SaveAsync(It.IsAny<WorkflowState>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.IsType<ProtoRunWorkflowInstanceResponse>(await harness.RunAsync(timeout.Token));

        harness.WorkflowRunner.Verify(x => x.RunAsync(
            harness.WorkflowGraph, initialState, It.IsAny<RunWorkflowOptions?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ImportState_WhenSavingImportedStateFails_PreservesCachedGraphAndState()
    {
        await using var harness = new WorkflowInstanceTestHarness();
        using var timeout = new CancellationTokenSource(TestTimeout);
        var initialState = WorkflowInstanceTestHarness.CreateState();
        var migratedState = WorkflowInstanceTestHarness.CreateState("definition-version-2", 2);
        var migratedGraph = WorkflowInstanceTestHarness.CreateWorkflowGraph(migratedState.DefinitionVersionId, migratedState.DefinitionVersion);
        harness.SetupExistingInstance(initialState);
        harness.WorkflowDefinitionService
            .Setup(x => x.FindWorkflowGraphAsync(
                It.Is<WorkflowDefinitionHandle>(handle => handle.DefinitionVersionId == migratedState.DefinitionVersionId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(migratedGraph);
        harness.WorkflowInstanceManager
            .Setup(x => x.SaveAsync(migratedState, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Import persistence failed."));

        var error = Assert.IsType<GrainErrorResponse>(await harness.ImportStateAsync(migratedState, timeout.Token));
        Assert.Contains("Import persistence failed.", error.Err);
        harness.WorkflowInstanceManager.Verify(x => x.SaveAsync(migratedState, It.IsAny<CancellationToken>()), Times.Once);
        Assert.IsType<ProtoRunWorkflowInstanceResponse>(await harness.RunAsync(timeout.Token));

        harness.WorkflowRunner.Verify(x => x.RunAsync(
            harness.WorkflowGraph, initialState, It.IsAny<RunWorkflowOptions?>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
