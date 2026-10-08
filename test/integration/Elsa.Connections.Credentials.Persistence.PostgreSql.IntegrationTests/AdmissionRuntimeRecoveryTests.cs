using System.Data.Common;
using Elsa.Workflows;
using Elsa.Workflows.Admission;
using Elsa.Workflows.Admission.WorkerProcess;
using Elsa.Workflows.Management;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Messages;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Connections.Credentials.Persistence.PostgreSql.IntegrationTests;

[Collection("Connections PostgreSQL")]
public sealed class AdmissionRuntimeRecoveryTests(PostgreSqlConnectionsFixture fixture)
{
    private readonly AdmissionRuntimeTestFixture _runtime = new(fixture);

    [Fact]
    public async Task UnknownSavedNotificationNeverRepeatsMaterializationOrNotifications()
    {
        await _runtime.RunAsync(async host =>
        {
            host.Probe.FailSavedNotification = true;
            await Assert.ThrowsAsync<IOException>(() => host.Execution.ExecuteAsync(host.AdmissionId));
            var record = (await host.Store.FindAsync(host.AdmissionId))!;
            Assert.Equal(AdmissionState.RecoveryRequired, record.State);
            Assert.NotNull(await host.Services.GetRequiredService<IWorkflowInstanceManager>().FindByIdAsync(record.WorkflowInstanceId!));
            Assert.Equal(1, host.Probe.Count("instanceInsertAttempts"));
            Assert.Equal(1, host.Probe.Count("savedNotifications"));
            Assert.Equal(0, host.Probe.Count("workflowExecuting"));
            await host.Execution.RecoverAsync(record.Id);
            Assert.Null(await host.Execution.ExecuteAsync(record.Id));
            Assert.Equal(1, host.Probe.Count("instanceInsertAttempts"));
            Assert.Equal(1, host.Probe.Count("savedNotifications"));
            Assert.Equal(0, host.Probe.Count("activityEffects"));
            await ObserveAsync("runtime-unknown-saved-notification", nameof(UnknownSavedNotificationNeverRepeatsMaterializationOrNotifications), "default",
                new() { ["instanceInsertAttempts"] = 1, ["savedNotifications"] = 1, ["workflowExecuting"] = 0, ["activityEffects"] = 0, ["recoveryRequired"] = true });
        });
    }

    [Fact]
    public async Task UnknownCommittedPermitCannotProduceCapabilityByReadbackOrRecovery()
    {
        var fault = new UnknownPermitCommit();
        await _runtime.RunAsync(async host =>
        {
            fault.Armed = true;
            await Assert.ThrowsAsync<IOException>(() => host.Execution.ExecuteAsync(host.AdmissionId));
            Assert.True(fault.ObservedCommittedAuthority);
            var record = (await host.Store.FindAsync(host.AdmissionId))!;
            Assert.Equal(AdmissionState.RecoveryRequired, record.State);
            Assert.True(record.AuthorityOutstanding);
            Assert.Equal(0, host.Probe.Count("StartAuthorized"));
            Assert.Equal(0, host.Probe.Count("AuthorityConsumed"));
            Assert.Equal(0, host.Probe.Count("workflowExecuting"));
            await host.Execution.RecoverAsync(record.Id);
            Assert.Null(await host.Execution.ExecuteAsync(record.Id));
            var client = await host.Services.GetRequiredService<IWorkflowRuntime>().CreateClientAsync(record.WorkflowInstanceId);
            await Assert.ThrowsAsync<InvalidOperationException>(() => client.RunInstanceAsync(new RunWorkflowInstanceRequest()));
            Assert.Equal(0, host.Probe.Count("activityEffects"));
            await ObserveAsync("runtime-unknown-permit-commit", nameof(UnknownCommittedPermitCannotProduceCapabilityByReadbackOrRecovery), "default",
                new() { ["committedAuthorityObserved"] = true, ["authorityConsumed"] = 0, ["workflowExecuting"] = 0, ["activityEffects"] = 0, ["recoveryRequired"] = true });
        }, fault);
    }

    [Theory]
    [InlineData("runtime-withdraw-before-authority", "StartPrepared", false)]
    [InlineData("runtime-withdraw-delayed-authority", "BeforeRunnerEntry", true)]
    public async Task WithdrawalHonorsRecordedAuthorityBoundaryAndHeldOwnerQuiescence(string caseId, string boundary, bool issued)
    {
        await _runtime.RunAsync(async host =>
        {
            var reached = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            host.Probe.Boundary = async current =>
            {
                if (current == boundary)
                {
                    reached.TrySetResult();
                    await release.Task.WaitAsync(TimeSpan.FromSeconds(30));
                }
            };
            var execution = host.Execution.ExecuteAsync(host.AdmissionId);
            try
            {
                await reached.Task.WaitAsync(TimeSpan.FromSeconds(30));
                var record = (await host.Store.FindAsync(host.AdmissionId))!;
                Assert.Equal(issued, record.AuthorityOutstanding);
                var subscription = (await host.Store.FindSubscriptionAsync(record.SubscriptionId))!;
                await host.Services.GetRequiredService<AdmissionBootstrapService>().WithdrawAndRetractAsync(subscription.Id, subscription.Revision);
                subscription = (await host.Store.FindSubscriptionAsync(record.SubscriptionId))!;
                Assert.False(subscription.Active);
                Assert.False((await host.Services.GetRequiredService<IWorkflowDefinitionService>().FindWorkflowDefinitionAsync(subscription.Configuration.DefinitionVersionId))!.IsPublished);
                await Assert.ThrowsAsync<InvalidOperationException>(() => host.Execution.ResolveAsync(record.Id, record.Revision,
                    AdmissionTerminalDisposition.Resolved, "fixture-delayed-owner", true, true));
                Assert.False(await host.Store.CleanupAsync(record.Id, record.Revision, subscription.Configuration.Policy.CleanupAuthority,
                    AdmissionWorkerHost.Now + subscription.Configuration.Policy.IdentityHorizon + TimeSpan.FromDays(1)));
                Assert.Equal(0, host.Probe.Count("workflowExecuting"));
                Assert.Equal(0, host.Probe.Count("activityEffects"));
                release.TrySetResult();
                if (issued)
                {
                    Assert.NotNull(await execution);
                }
                else
                {
                    await Assert.ThrowsAsync<InvalidOperationException>(() => execution);
                }
            }
            finally
            {
                release.TrySetResult();
                try
                {
                    await execution;
                }
                catch (InvalidOperationException) when (!issued)
                {
                    // Withdrawal before issuance is an expected fail-closed execution result.
                }
            }
            Assert.Equal(issued ? 1 : 0, host.Probe.Count("activityEffects"));
            Assert.Equal(issued ? AdmissionState.Terminal : AdmissionState.RecoveryRequired, (await host.Store.FindAsync(host.AdmissionId))!.State);
            await ObserveAsync(caseId, nameof(WithdrawalHonorsRecordedAuthorityBoundaryAndHeldOwnerQuiescence), caseId,
                new() { ["authorityIssuedBeforeWithdrawal"] = issued, ["ownerResolutionDenied"] = true, ["cleanupDeniedWhileHeld"] = true,
                    ["subscriptionWithdrawn"] = true, ["definitionRetracted"] = true, ["activityEffects"] = issued ? 1 : 0 });
        });
    }

    [Fact]
    public async Task FailedActualRetractionRetainsAuthoritativeWithdrawalAndReconciliation()
    {
        await _runtime.RunAsync(async host =>
        {
            host.Probe.FailRetract = true;
            var subscription = (await host.Store.FindSubscriptionAsync(AdmissionWorkerHost.Configuration().Id))!;
            await Assert.ThrowsAsync<IOException>(() => host.Services.GetRequiredService<AdmissionBootstrapService>()
                .WithdrawAndRetractAsync(subscription.Id, subscription.Revision));
            var after = (await host.Store.FindSubscriptionAsync(subscription.Id))!;
            Assert.False(after.Active);
            Assert.Equal("withdrawal-external-mutation-incomplete", after.ReconciliationCode);
            Assert.Equal(1, host.Probe.Count("definitionRetracting"));
            Assert.True((await host.Services.GetRequiredService<IWorkflowDefinitionService>().FindWorkflowDefinitionAsync(subscription.Configuration.DefinitionVersionId))!.IsPublished);
            Assert.Null(await host.Execution.ExecuteAsync(host.AdmissionId));
            Assert.Equal(0, host.Probe.Count("instanceInsertAttempts"));
            Assert.Equal(0, host.Probe.Count("activityEffects"));
            await ObserveAsync("runtime-withdraw-retract-failure", nameof(FailedActualRetractionRetainsAuthoritativeWithdrawalAndReconciliation), "default",
                new() { ["subscriptionWithdrawn"] = true, ["reconciliationRequired"] = true, ["definitionStillPublished"] = true,
                    ["instanceInsertAttempts"] = 0, ["activityEffects"] = 0 });
        });
    }

    private Task ObserveAsync(string caseId, string method, string parameterId, Dictionary<string, object> facts) =>
        AdmissionProofObservation.WriteAsync(fixture, caseId, $"{typeof(AdmissionRuntimeRecoveryTests).FullName}.{method}", parameterId, [],
            new Dictionary<string, bool> { ["behaviorAssertionsPassed"] = true }, facts);

    private sealed class UnknownPermitCommit : DbTransactionInterceptor
    {
        public bool Armed { get; set; }
        public bool ObservedCommittedAuthority { get; private set; }
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
        {
            if (Armed && eventData.Context!.ChangeTracker.Entries<AdmissionRecord>().Any(entry =>
                    entry.Entity.State == AdmissionState.StartAuthorized && entry.Entity.AuthorityOutstanding))
            {
                Armed = false;
                ObservedCommittedAuthority = true;
                throw new IOException("fixture_permit_commit_outcome_unknown");
            }
            return Task.CompletedTask;
        }
    }
}
