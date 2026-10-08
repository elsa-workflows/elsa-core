using Elsa.Workflows;
using Elsa.Workflows.Admission;
using Elsa.Workflows.Admission.WorkerProcess;
using Elsa.Workflows.Management;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Messages;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Connections.Credentials.Persistence.PostgreSql.IntegrationTests;

[Collection("Connections PostgreSQL")]
public sealed class AdmissionRuntimeControlTests(PostgreSqlConnectionsFixture fixture)
{
    private readonly AdmissionRuntimeTestFixture _runtime = new(fixture);

    [Fact]
    public async Task UnownedClientCannotImportStateWhoseActualTargetIsOwned()
    {
        await _runtime.RunAsync(async host =>
        {
            host.Probe.Outcome = "suspended";
            var owned = (await host.Execution.ExecuteAsync(host.AdmissionId))!;
            var instances = host.Services.GetRequiredService<IWorkflowInstanceManager>();
            var serializer = host.Services.GetRequiredService<IWorkflowStateSerializer>();
            var original = serializer.Serialize((await instances.FindByIdAsync(owned.WorkflowInstanceId))!.WorkflowState);
            var forged = serializer.Deserialize(original);
            forged.Output["Imported"] = "synthetic-forged-owned-state";
            var client = await host.Services.GetRequiredService<IWorkflowRuntime>().CreateClientAsync("fixture-unowned-import-source");
            var writes = host.Probe.Count("instanceWriteAttempts");
            var bookmarkWrites = host.Probe.Count("bookmarkSaveCalls");
            await Assert.ThrowsAsync<InvalidOperationException>(() => client.ImportStateAsync(forged));
            Assert.Equal(writes, host.Probe.Count("instanceWriteAttempts"));
            Assert.Equal(bookmarkWrites, host.Probe.Count("bookmarkSaveCalls"));
            Assert.Equal(original, serializer.Serialize((await instances.FindByIdAsync(owned.WorkflowInstanceId))!.WorkflowState));
            Assert.Null(await instances.FindByIdAsync(client.WorkflowInstanceId));
            Assert.Equal(0, host.Probe.Count("workflowCancelling"));
            Assert.Equal(1, host.Probe.Count("workflowExecuting"));
            Assert.Equal(1, host.Probe.Count("workflowStarted"));
            Assert.Equal(1, host.Probe.Count("activityEffects"));
            await ObserveAsync("runtime-import-foreign-owned-target", nameof(UnownedClientCannotImportStateWhoseActualTargetIsOwned), "default",
                new() { ["ownedTargetStateUnchanged"] = true, ["unownedSourceNotCreated"] = true,
                    ["additionalInstanceWrites"] = 0, ["additionalBookmarkWrites"] = 0,
                    ["additionalStartNotifications"] = 0, ["cancellationNotifications"] = 0, ["activityEffects"] = 1 });
        });
    }

    [Theory]
    [InlineData("runtime-unowned-control-cancel", "cancel")]
    [InlineData("runtime-unowned-control-delete", "delete")]
    [InlineData("runtime-unowned-control-import", "import")]
    public async Task ActualUnownedLocalClientControlsRemainCompatibleWithoutChangingOwnedState(string caseId, string scenario)
    {
        await _runtime.RunAsync(async host =>
        {
            host.Probe.Outcome = "suspended";
            var owned = (await host.Execution.ExecuteAsync(host.AdmissionId))!;
            var instances = host.Services.GetRequiredService<IWorkflowInstanceManager>();
            var serializer = host.Services.GetRequiredService<IWorkflowStateSerializer>();
            var originalOwned = serializer.Serialize((await instances.FindByIdAsync(owned.WorkflowInstanceId))!.WorkflowState);
            var client = await host.Services.GetRequiredService<IWorkflowRuntime>().CreateClientAsync("fixture-unowned-control");
            var unowned = await client.CreateAndRunInstanceAsync(new CreateAndRunWorkflowInstanceRequest
            {
                WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionVersionId(AdmissionRuntimeHost.Artifact().Id)
            });
            Assert.Equal(WorkflowSubStatus.Suspended, unowned.SubStatus);
            Assert.Null(await host.Store.FindByInstanceAsync(unowned.WorkflowInstanceId));
            Assert.Equal(2, host.Probe.Count("activityEffects"));
            var before = (await instances.FindByIdAsync(unowned.WorkflowInstanceId))!;
            switch (scenario)
            {
                case "cancel":
                    await client.CancelAsync();
                    var cancelled = (await instances.FindByIdAsync(unowned.WorkflowInstanceId))!;
                    Assert.Equal(WorkflowStatus.Finished, cancelled.Status);
                    Assert.Equal(WorkflowSubStatus.Cancelled, cancelled.SubStatus);
                    Assert.Equal(1, host.Probe.Count("workflowCancelling"));
                    break;
                case "delete":
                    Assert.True(await client.DeleteAsync());
                    Assert.Null(await instances.FindByIdAsync(unowned.WorkflowInstanceId));
                    Assert.False(await client.InstanceExistsAsync());
                    Assert.Equal(1, host.Probe.Count("workflowCancelling"));
                    break;
                case "import":
                    var imported = serializer.Deserialize(serializer.Serialize(before.WorkflowState));
                    imported.Output["Imported"] = "synthetic-unowned-import";
                    await client.ImportStateAsync(imported);
                    var reloaded = (await instances.FindByIdAsync(unowned.WorkflowInstanceId))!;
                    Assert.Equal("synthetic-unowned-import", reloaded.WorkflowState.Output["Imported"].ToString());
                    Assert.Equal(WorkflowSubStatus.Suspended, reloaded.SubStatus);
                    Assert.Equal(0, host.Probe.Count("workflowCancelling"));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(scenario));
            }
            Assert.Equal(originalOwned, serializer.Serialize((await instances.FindByIdAsync(owned.WorkflowInstanceId))!.WorkflowState));
            Assert.Equal(AdmissionState.ExecutionObserved, (await host.Store.FindAsync(host.AdmissionId))!.State);
            Assert.Equal(2, host.Probe.Count("activityEffects"));
            Assert.Equal(0, host.Probe.Count("activityResumes"));
            await ObserveAsync(caseId, nameof(ActualUnownedLocalClientControlsRemainCompatibleWithoutChangingOwnedState), caseId,
                new() { ["unownedControlSucceeded"] = true, ["ownedStateUnchanged"] = true,
                    ["cancellationNotifications"] = scenario == "import" ? 0 : 1, ["activityEffects"] = 2, ["activityResumes"] = 0 });
        });
    }

    private Task ObserveAsync(string caseId, string method, string parameterId, Dictionary<string, object> facts) =>
        AdmissionProofObservation.WriteAsync(fixture, caseId, $"{typeof(AdmissionRuntimeControlTests).FullName}.{method}", parameterId, [],
            new Dictionary<string, bool> { ["behaviorAssertionsPassed"] = true }, facts);
}
