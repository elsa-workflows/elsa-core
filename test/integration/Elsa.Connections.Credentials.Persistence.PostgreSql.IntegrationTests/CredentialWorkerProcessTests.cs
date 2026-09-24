using System.Text.Json;

namespace Elsa.Connections.Credentials.Persistence.PostgreSql.IntegrationTests;

[Collection("Connections PostgreSQL")]
public sealed class CredentialWorkerProcessTests(PostgreSqlConnectionsFixture fixture)
{
    private readonly WorkerProcessRunner _workers = new();

    [Fact]
    public async Task HostedWorkersInSeparateProcesses_OnlyOneClaimsRefresh_AndRepeatedWakesStaySafe()
    {
        await fixture.ResetSchemaAsync();
        await using var provider = await SyntheticOAuthServer.StartAsync();
        provider.BlockRefresh = true;
        provider.IssueRefreshToken(ProcessTestEnvironment.RefreshMarker);
        var environment = await MigrateAsync(provider);
        var (connectionId, originalGenerationId, connectResult) = await ConnectAsync(environment);
        var dueAt = ProcessTestEnvironment.InitialTime.AddHours(1);
        var workerEnvironment = ProcessTestEnvironment.With(environment,
            ("ELSA_TEST_ALLOW_BACKGROUND", "true"),
            ("ELSA_TEST_NOW_UTC", dueAt.ToString("O")),
            ("ELSA_TEST_WORKER_INTERVAL_MS", "50"));
        var barrierDirectory = Path.Combine(Path.GetTempPath(), $"elsa-8378-worker-race-{Guid.NewGuid():N}");
        Directory.CreateDirectory(barrierDirectory);

        var firstEnvironment = ProcessTestEnvironment.With(workerEnvironment,
            ("ELSA_TEST_BARRIER_METHOD", "TryClaimRefreshAsync"),
            ("ELSA_TEST_BARRIER_PARTICIPANT", "hosted-first"),
            ("ELSA_TEST_BARRIER_DIRECTORY", barrierDirectory),
            ("ELSA_TEST_REJECTED_CLAIM_PATH", Path.Join(barrierDirectory, "claim-rejected")));
        var secondEnvironment = ProcessTestEnvironment.With(workerEnvironment,
            ("ELSA_TEST_BARRIER_METHOD", "TryClaimRefreshAsync"),
            ("ELSA_TEST_BARRIER_PARTICIPANT", "hosted-second"),
            ("ELSA_TEST_BARRIER_DIRECTORY", barrierDirectory),
            ("ELSA_TEST_REJECTED_CLAIM_PATH", Path.Join(barrierDirectory, "claim-rejected")));
        await using var firstWorker = _workers.Start(["hosted-worker"], firstEnvironment);
        ProcessRun? secondWorker = null;
        ProcessRunResult? firstOutput = null;
        ProcessRunResult? secondOutput = null;
        try
        {
            await firstWorker.WaitForLineAsync("BARRIER_READY:hosted-first", TimeSpan.FromSeconds(30));
            secondWorker = _workers.Start(["hosted-worker"], secondEnvironment);
            await secondWorker.WaitForLineAsync("BARRIER_READY:hosted-second", TimeSpan.FromSeconds(30));
            await File.WriteAllTextAsync(Path.Join(barrierDirectory, "go"), "go");
            await provider.WaitForRefreshAsync(TimeSpan.FromSeconds(30));
            await WaitForFileAsync(Path.Join(barrierDirectory, "claim-rejected"), TimeSpan.FromSeconds(30));
            provider.ReleaseRefresh();

            var finalState = await WaitForOperationStatusAsync(connectionId, originalGenerationId, "Completed", workerEnvironment);
            Assert.Equal("Active", finalState.GetProperty("status").GetString());
            Assert.NotEqual(originalGenerationId, finalState.GetProperty("currentGenerationId").GetString());
            Assert.Equal(1, provider.RefreshCalls);
            Assert.Equal(1, provider.AcceptedRefreshCalls);

            // The active generation now expires in the future, so repeated worker wakes only see empty pages.
            await firstWorker.WaitForLineAsync("DUE_PAGE:0", TimeSpan.FromSeconds(30));
            await firstWorker.WaitForLineAsync("DUE_PAGE:0", TimeSpan.FromSeconds(30));
            Assert.Equal(1, provider.RefreshCalls);
        }
        finally
        {
            provider.ReleaseRefresh();
            firstOutput = await firstWorker.TerminateAsync();
            if (secondWorker != null)
                secondOutput = await secondWorker.TerminateAsync();
            Directory.Delete(barrierDirectory, recursive: true);
        }

        var otherScope = ProcessTestEnvironment.With(workerEnvironment, ("ELSA_TEST_TENANT_ID", "other-tenant"));
        await using var wrongScopeWorker = _workers.Start(["hosted-worker"], otherScope);
        await wrongScopeWorker.WaitForLineAsync("DUE_PAGE:0", TimeSpan.FromSeconds(30));
        var wrongScopeOutput = await wrongScopeWorker.TerminateAsync();
        Assert.Equal(1, provider.RefreshCalls);

        AssertSafeOutput(connectResult, ProcessTestEnvironment.AccessMarker, ProcessTestEnvironment.RefreshMarker);
        AssertSafeOutput(firstOutput!, ProcessTestEnvironment.AccessMarker, ProcessTestEnvironment.RefreshMarker);
        AssertSafeOutput(secondOutput!, ProcessTestEnvironment.AccessMarker, ProcessTestEnvironment.RefreshMarker);
        AssertSafeOutput(wrongScopeOutput, ProcessTestEnvironment.AccessMarker, ProcessTestEnvironment.RefreshMarker);
    }

    [Fact]
    public async Task HostedWorkerRestartAfterProviderCallBoundary_HoldsUnknownOutcomeWithoutReplay()
    {
        await fixture.ResetSchemaAsync();
        await using var provider = await SyntheticOAuthServer.StartAsync();
        provider.IssueRefreshToken(ProcessTestEnvironment.RefreshMarker);
        var environment = await MigrateAsync(provider);
        var (connectionId, originalGenerationId, connectResult) = await ConnectAsync(environment);
        var dueAt = ProcessTestEnvironment.InitialTime.AddHours(1);
        var workerEnvironment = ProcessTestEnvironment.With(environment,
            ("ELSA_TEST_ALLOW_BACKGROUND", "true"),
            ("ELSA_TEST_NOW_UTC", dueAt.ToString("O")),
            ("ELSA_TEST_WORKER_INTERVAL_MS", "50"),
            ("ELSA_TEST_CRASH_AFTER", "refresh-provider-started"));

        await using var crashedWorker = _workers.Start(["hosted-worker"], workerEnvironment);
        await crashedWorker.WaitForLineAsync("BOUNDARY:refresh-provider-started", TimeSpan.FromSeconds(30));
        var crashOutput = await crashedWorker.TerminateAsync();
        Assert.Equal(0, provider.RefreshCalls);

        var recoveryEnvironment = ProcessTestEnvironment.With(workerEnvironment,
            ("ELSA_TEST_CRASH_AFTER", ""),
            ("ELSA_TEST_NOW_UTC", dueAt.AddMinutes(3).ToString("O")));
        await using var resumedWorker = _workers.Start(["hosted-worker"], recoveryEnvironment);
        await resumedWorker.WaitForLineAsync("DUE_PAGE:2", TimeSpan.FromSeconds(30));
        await resumedWorker.WaitForLineAsync("DUE_PAGE:1", TimeSpan.FromSeconds(30));
        await resumedWorker.WaitForLineAsync("DUE_PAGE:1", TimeSpan.FromSeconds(30));
        var resumedOutput = await resumedWorker.TerminateAsync();
        var finalInspect = await _workers.RunAsync(["inspect", connectionId, originalGenerationId], recoveryEnvironment);
        var finalState = finalInspect.ReadResult();

        Assert.Equal("RecoveryRequired", finalState.GetProperty("status").GetString());
        Assert.Equal("RecoveryRequired", finalState.GetProperty("operationStatus").GetString());
        Assert.Equal(originalGenerationId, finalState.GetProperty("currentGenerationId").GetString());
        Assert.Equal(0, provider.RefreshCalls);
        AssertSafeOutput(connectResult, ProcessTestEnvironment.AccessMarker, ProcessTestEnvironment.RefreshMarker);
        AssertSafeOutput(crashOutput, ProcessTestEnvironment.AccessMarker, ProcessTestEnvironment.RefreshMarker);
        AssertSafeOutput(resumedOutput, ProcessTestEnvironment.AccessMarker, ProcessTestEnvironment.RefreshMarker);
        AssertSafeOutput(finalInspect, ProcessTestEnvironment.AccessMarker, ProcessTestEnvironment.RefreshMarker);
    }

    [Fact]
    public async Task HostedWorkerDoesNotScheduleApiKeyForOAuthRefresh()
    {
        await fixture.ResetSchemaAsync();
        await using var provider = await SyntheticOAuthServer.StartAsync();
        var environment = await MigrateAsync(provider);
        var apiKey = $"synthetic-api-key-{Guid.NewGuid():N}";
        var connectOutput = await _workers.RunAsync(["connect-api-key"],
            ProcessTestEnvironment.With(environment, ("ELSA_TEST_API_KEY", apiKey)));
        Assert.Equal(0, connectOutput.ExitCode);
        var connectionId = connectOutput.ReadResult().GetProperty("connectionId").GetString();
        Assert.False(string.IsNullOrWhiteSpace(connectionId));

        var dueAt = ProcessTestEnvironment.InitialTime.AddHours(1);
        var workerEnvironment = ProcessTestEnvironment.With(environment,
            ("ELSA_TEST_NOW_UTC", dueAt.ToString("O")),
            ("ELSA_TEST_WORKER_INTERVAL_MS", "50"));
        await using var worker = _workers.Start(["hosted-worker"], workerEnvironment);
        await worker.WaitForLineAsync("DUE_PAGE:0", TimeSpan.FromSeconds(30));
        await worker.WaitForLineAsync("DUE_PAGE:0", TimeSpan.FromSeconds(30));
        var workerOutput = await worker.TerminateAsync();

        Assert.Equal(0, provider.RefreshCalls);
        AssertSafeOutput(connectOutput, apiKey);
        AssertSafeOutput(workerOutput, apiKey);
    }

    private async Task<JsonElement> WaitForOperationStatusAsync(
        string connectionId,
        string generationId,
        string expectedStatus,
        IReadOnlyDictionary<string, string> environment)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(30);
        while (DateTimeOffset.UtcNow < deadline)
        {
            var result = await _workers.RunAsync(["inspect", connectionId, generationId], environment);
            AssertSafeOutput(result, ProcessTestEnvironment.AccessMarker, ProcessTestEnvironment.RefreshMarker);
            var state = result.ReadResult();
            if (state.GetProperty("operationStatus").GetString() == expectedStatus)
                return state;

            await Task.Delay(50);
        }

        throw new TimeoutException("hosted_worker_operation_status_timeout");
    }

    private static async Task WaitForFileAsync(string path, TimeSpan timeout)
    {
        using var cancellation = new CancellationTokenSource(timeout);
        while (!File.Exists(path))
            await Task.Delay(10, cancellation.Token);
    }

    [Fact]
    public async Task SeparateProcesses_RefreshOneSingleUseToken_AndEmitOnlySafeMetadata()
    {
        await fixture.ResetSchemaAsync();
        await using var provider = await SyntheticOAuthServer.StartAsync();
        provider.BlockRefresh = true;
        provider.IssueRefreshToken(ProcessTestEnvironment.RefreshMarker);
        var environment = await MigrateAsync(provider);
        var (connectionId, originalGenerationId, connectResult) = await ConnectAsync(environment);

        var firstWorker = _workers.Start(["refresh", connectionId], environment);
        await using var firstProcess = firstWorker;
        ProcessRunResult? secondResult = null;
        ProcessRunResult? firstResult = null;
        try
        {
            await provider.WaitForRefreshAsync(TimeSpan.FromSeconds(30));
            secondResult = await _workers.RunAsync(["refresh", connectionId], environment);
        }
        finally
        {
            provider.ReleaseRefresh();
        }

        firstResult = await firstProcess.CompleteAsync();
        Assert.NotEqual(firstResult.ProcessId, secondResult!.ProcessId);
        Assert.True(firstResult.ReadResult().GetProperty("succeeded").GetBoolean());
        Assert.False(secondResult.ReadResult().GetProperty("succeeded").GetBoolean());
        Assert.Equal("refresh_conflict", secondResult.ReadResult().GetProperty("safeErrorCode").GetString());
        Assert.Equal(1, provider.RefreshCalls);
        Assert.Equal(1, provider.AcceptedRefreshCalls);
        Assert.True(provider.IsRefreshTokenConsumed(ProcessTestEnvironment.RefreshMarker));

        var inspect = await _workers.RunAsync(["inspect", connectionId, originalGenerationId], environment);
        Assert.Equal(0, inspect.ExitCode);
        var state = inspect.ReadResult();
        Assert.Equal("Active", state.GetProperty("status").GetString());
        Assert.Equal("Completed", state.GetProperty("operationStatus").GetString());
        Assert.NotEqual(originalGenerationId, state.GetProperty("currentGenerationId").GetString());
        AssertSafeOutput(connectResult, ProcessTestEnvironment.AccessMarker, ProcessTestEnvironment.RefreshMarker);
        AssertSafeOutput(firstResult, ProcessTestEnvironment.AccessMarker, ProcessTestEnvironment.RefreshMarker);
        AssertSafeOutput(secondResult, ProcessTestEnvironment.AccessMarker, ProcessTestEnvironment.RefreshMarker);
        AssertSafeOutput(inspect, ProcessTestEnvironment.AccessMarker, ProcessTestEnvironment.RefreshMarker);
    }

    [Theory]
    [InlineData("refresh-claimed")]
    [InlineData("refresh-provider-started")]
    [InlineData("refresh-staged")]
    [InlineData("refresh-published")]
    public async Task RestartAfterOwnedWorkerExit_RecoversOnlyDurableSafeRefreshBoundaries(string boundary)
    {
        await fixture.ResetSchemaAsync();
        await using var provider = await SyntheticOAuthServer.StartAsync();
        provider.IssueRefreshToken(ProcessTestEnvironment.RefreshMarker);
        var environment = await MigrateAsync(provider);
        var (connectionId, originalGenerationId, _) = await ConnectAsync(environment);

        await using var crashedWorker = _workers.Start(
            ["refresh", connectionId],
            ProcessTestEnvironment.With(environment, ("ELSA_TEST_CRASH_AFTER", boundary)));
        await crashedWorker.WaitForLineAsync($"BOUNDARY:{boundary}", TimeSpan.FromSeconds(30));
        var crashOutput = await crashedWorker.TerminateAsync();
        AssertSafeOutput(crashOutput, ProcessTestEnvironment.AccessMarker, ProcessTestEnvironment.RefreshMarker);

        if (boundary != "refresh-published")
        {
            var liveLease = await _workers.RunAsync(["reconcile", connectionId], environment);
            Assert.False(liveLease.ReadResult().GetProperty("succeeded").GetBoolean());
            Assert.Equal("operation_in_progress", liveLease.ReadResult().GetProperty("safeErrorCode").GetString());
        }

        var recoveryTime = ProcessTestEnvironment.With(
            environment,
            ("ELSA_TEST_NOW_UTC", ProcessTestEnvironment.InitialTime.AddMinutes(3).ToString("O")));
        var recovered = await _workers.RunAsync(["reconcile", connectionId], recoveryTime);
        var recovery = recovered.ReadResult();
        var inspect = await _workers.RunAsync(["inspect", connectionId, originalGenerationId], recoveryTime);
        var state = inspect.ReadResult();

        switch (boundary)
        {
            case "refresh-claimed":
                Assert.False(recovery.GetProperty("succeeded").GetBoolean());
                Assert.Equal("refresh_not_started", recovery.GetProperty("safeErrorCode").GetString());
                Assert.Equal("Active", state.GetProperty("status").GetString());
                Assert.Equal("Completed", state.GetProperty("operationStatus").GetString());
                Assert.Equal(originalGenerationId, state.GetProperty("currentGenerationId").GetString());
                Assert.Equal(0, provider.RefreshCalls);
                break;
            case "refresh-provider-started":
                Assert.False(recovery.GetProperty("succeeded").GetBoolean());
                Assert.Equal("refresh_outcome_unknown", recovery.GetProperty("safeErrorCode").GetString());
                Assert.Equal("RecoveryRequired", state.GetProperty("status").GetString());
                Assert.Equal("RecoveryRequired", state.GetProperty("operationStatus").GetString());
                Assert.Equal(originalGenerationId, state.GetProperty("currentGenerationId").GetString());
                Assert.Equal(0, provider.RefreshCalls);
                break;
            case "refresh-staged":
                Assert.True(recovery.GetProperty("succeeded").GetBoolean());
                Assert.Equal("Active", state.GetProperty("status").GetString());
                Assert.Equal("Completed", state.GetProperty("operationStatus").GetString());
                Assert.NotEqual(originalGenerationId, state.GetProperty("currentGenerationId").GetString());
                Assert.Equal(1, provider.RefreshCalls);
                Assert.True(provider.IsRefreshTokenConsumed(ProcessTestEnvironment.RefreshMarker));
                break;
            case "refresh-published":
                Assert.True(recovery.GetProperty("succeeded").GetBoolean());
                Assert.Equal("Active", state.GetProperty("status").GetString());
                Assert.Equal("Completed", state.GetProperty("operationStatus").GetString());
                Assert.NotEqual(originalGenerationId, state.GetProperty("currentGenerationId").GetString());
                Assert.Equal(1, provider.RefreshCalls);
                Assert.True(provider.IsRefreshTokenConsumed(ProcessTestEnvironment.RefreshMarker));
                break;
            default:
                throw new InvalidOperationException("unknown_refresh_boundary");
        }

        AssertSafeOutput(recovered, ProcessTestEnvironment.AccessMarker, ProcessTestEnvironment.RefreshMarker);
        AssertSafeOutput(inspect, ProcessTestEnvironment.AccessMarker, ProcessTestEnvironment.RefreshMarker);
    }

    [Fact]
    public async Task DisconnectWhileProviderCallIsInFlight_PreventsStaleWorkerAndExpiredLeaseRecoveryFromReactivating()
    {
        await fixture.ResetSchemaAsync();
        await using var provider = await SyntheticOAuthServer.StartAsync();
        provider.BlockRefresh = true;
        provider.IssueRefreshToken(ProcessTestEnvironment.RefreshMarker);
        var environment = await MigrateAsync(provider);
        var (connectionId, originalGenerationId, _) = await ConnectAsync(environment);

        await using var staleRefreshWorker = _workers.Start(["refresh", connectionId], environment);
        ProcessRunResult? disconnect = null;
        ProcessRunResult? liveLeaseReconciliation = null;
        ProcessRunResult? disconnectedInspect = null;
        try
        {
            await provider.WaitForRefreshAsync(TimeSpan.FromSeconds(30));
            disconnect = await _workers.RunAsync(["disconnect", connectionId], environment);
            Assert.True(disconnect.ReadResult().GetProperty("accepted").GetBoolean());
            liveLeaseReconciliation = await _workers.RunAsync(["reconcile", connectionId], environment);
            Assert.False(liveLeaseReconciliation.ReadResult().GetProperty("succeeded").GetBoolean());
            Assert.Equal("operation_in_progress", liveLeaseReconciliation.ReadResult().GetProperty("safeErrorCode").GetString());
            Assert.Equal(1, provider.RefreshCalls);
            disconnectedInspect = await _workers.RunAsync(["inspect", connectionId, originalGenerationId], environment);
            var disconnectedState = disconnectedInspect.ReadResult();
            Assert.Equal("Disconnected", disconnectedState.GetProperty("status").GetString());
            Assert.Equal("ProviderCallStarted", disconnectedState.GetProperty("operationStatus").GetString());
            Assert.Equal(originalGenerationId, disconnectedState.GetProperty("currentGenerationId").GetString());
        }
        finally
        {
            provider.ReleaseRefresh();
        }

        var staleRefresh = await staleRefreshWorker.CompleteAsync();
        Assert.False(staleRefresh.ReadResult().GetProperty("succeeded").GetBoolean());
        Assert.Equal(1, provider.RefreshCalls);
        Assert.Equal(1, provider.AcceptedRefreshCalls);
        Assert.True(provider.IsRefreshTokenConsumed(ProcessTestEnvironment.RefreshMarker));

        var afterStaleCompletion = await _workers.RunAsync(["inspect", connectionId, originalGenerationId], environment);
        var staleState = afterStaleCompletion.ReadResult();
        Assert.Equal("Disconnected", staleState.GetProperty("status").GetString());
        Assert.Equal("RecoveryRequired", staleState.GetProperty("operationStatus").GetString());
        Assert.Equal(originalGenerationId, staleState.GetProperty("currentGenerationId").GetString());

        var expiredLease = ProcessTestEnvironment.With(
            environment,
            ("ELSA_TEST_NOW_UTC", ProcessTestEnvironment.InitialTime.AddMinutes(3).ToString("O")));
        var reconciliation = await _workers.RunAsync(["reconcile", connectionId], expiredLease);
        Assert.False(reconciliation.ReadResult().GetProperty("succeeded").GetBoolean());
        Assert.Equal("recovery_required", reconciliation.ReadResult().GetProperty("safeErrorCode").GetString());
        Assert.Equal(1, provider.RefreshCalls);

        var finalInspect = await _workers.RunAsync(["inspect", connectionId, originalGenerationId], expiredLease);
        var finalState = finalInspect.ReadResult();
        Assert.Equal("Disconnected", finalState.GetProperty("status").GetString());
        Assert.Equal("RecoveryRequired", finalState.GetProperty("operationStatus").GetString());
        Assert.Equal(originalGenerationId, finalState.GetProperty("currentGenerationId").GetString());
        var plannedGenerationId = finalState.GetProperty("plannedGenerationId").GetString();
        Assert.False(string.IsNullOrWhiteSpace(plannedGenerationId));
        Assert.NotEqual(originalGenerationId, plannedGenerationId);
        Assert.Equal(plannedGenerationId, finalState.GetProperty("stagedGenerationId").GetString());
        Assert.True(finalState.GetProperty("generationAvailable").GetBoolean());

        var stagedInspect = await _workers.RunAsync(["inspect", connectionId, plannedGenerationId!], expiredLease);
        Assert.True(stagedInspect.ReadResult().GetProperty("generationAvailable").GetBoolean());

        AssertSafeOutput(disconnect!, ProcessTestEnvironment.AccessMarker, ProcessTestEnvironment.RefreshMarker);
        AssertSafeOutput(liveLeaseReconciliation!, ProcessTestEnvironment.AccessMarker, ProcessTestEnvironment.RefreshMarker);
        AssertSafeOutput(disconnectedInspect!, ProcessTestEnvironment.AccessMarker, ProcessTestEnvironment.RefreshMarker);
        AssertSafeOutput(staleRefresh, ProcessTestEnvironment.AccessMarker, ProcessTestEnvironment.RefreshMarker);
        AssertSafeOutput(afterStaleCompletion, ProcessTestEnvironment.AccessMarker, ProcessTestEnvironment.RefreshMarker);
        AssertSafeOutput(reconciliation, ProcessTestEnvironment.AccessMarker, ProcessTestEnvironment.RefreshMarker);
        AssertSafeOutput(finalInspect, ProcessTestEnvironment.AccessMarker, ProcessTestEnvironment.RefreshMarker);
        AssertSafeOutput(stagedInspect, ProcessTestEnvironment.AccessMarker, ProcessTestEnvironment.RefreshMarker);
    }

    [Theory]
    [InlineData("revocation", false)]
    [InlineData("revocation", true)]
    [InlineData("uninstall", false)]
    [InlineData("uninstall", true)]
    public async Task ProviderSuccessBeforeLocalCompletion_IsUnknownOrSameOperationIdRetryOnly(string kind, bool stableOperationId)
    {
        await fixture.ResetSchemaAsync();
        await using var provider = await SyntheticOAuthServer.StartAsync();
        provider.StableRevocationIdempotency = stableOperationId;
        provider.StableUninstallIdempotency = stableOperationId;
        provider.IssueRefreshToken(ProcessTestEnvironment.RefreshMarker);
        var environment = ProcessTestEnvironment.With(
            await MigrateAsync(provider),
            ("ELSA_TEST_REVOCATION_IDEMPOTENT", stableOperationId.ToString()),
            ("ELSA_TEST_UNINSTALL_IDEMPOTENT", stableOperationId.ToString()));
        var (connectionId, generationId, _) = await ConnectAsync(environment);
        Assert.Equal(0, (await _workers.RunAsync(["disconnect", connectionId], environment)).ExitCode);

        var queueArguments = kind == "revocation"
            ? new[] { "request-revocation", connectionId, ProcessTestEnvironment.TenantId, ProcessTestEnvironment.EnvironmentId, generationId }
            : new[] { "request-uninstall", connectionId };
        var queued = await _workers.RunAsync(queueArguments, environment);
        Assert.Equal(0, queued.ExitCode);
        var operationId = queued.ReadResult().GetProperty("operationId").GetString();
        Assert.False(string.IsNullOrWhiteSpace(operationId));

        await using var providerCallWorker = _workers.Start(
            ["reconcile-offboarding", connectionId],
            ProcessTestEnvironment.With(environment, ("ELSA_TEST_CRASH_AFTER", "offboarding-provider-success")));
        await providerCallWorker.WaitForLineAsync("BOUNDARY:offboarding-provider-success", TimeSpan.FromSeconds(30));
        var crashOutput = await providerCallWorker.TerminateAsync();
        AssertSafeOutput(crashOutput, ProcessTestEnvironment.AccessMarker, ProcessTestEnvironment.RefreshMarker);

        Assert.Equal(1, kind == "revocation" ? provider.RevocationCalls : provider.UninstallCalls);
        Assert.Equal(1, kind == "revocation" ? provider.RevocationEffects : provider.UninstallEffects);

        var beforeExpiry = await _workers.RunAsync(["reconcile-offboarding", connectionId], environment);
        Assert.True(beforeExpiry.ReadResult().GetProperty("accepted").GetBoolean());
        Assert.Equal(1, kind == "revocation" ? provider.RevocationCalls : provider.UninstallCalls);

        var recoveryEnvironment = ProcessTestEnvironment.With(
            environment,
            ("ELSA_TEST_NOW_UTC", ProcessTestEnvironment.InitialTime.AddMinutes(3).ToString("O")));
        var recoveryResult = await _workers.RunAsync(["reconcile-offboarding", connectionId], recoveryEnvironment);
        var recovery = recoveryResult.ReadResult();
        var inspect = await _workers.RunAsync(["inspect", connectionId, generationId], recoveryEnvironment);
        var state = inspect.ReadResult();
        var operations = state.GetProperty("operations").EnumerateArray().ToArray();
        var operation = Assert.Single(operations, value => value.GetProperty("id").GetString() == operationId);
        Assert.Equal(operationId, operation.GetProperty("id").GetString());

        if (stableOperationId)
        {
            Assert.True(recovery.GetProperty("accepted").GetBoolean());
            Assert.Equal("Completed", operation.GetProperty("status").GetString());
            Assert.Equal(2, kind == "revocation" ? provider.RevocationCalls : provider.UninstallCalls);
            Assert.Equal(1, kind == "revocation" ? provider.RevocationEffects : provider.UninstallEffects);
            var operationIds = kind == "revocation" ? provider.RevocationCallIds : provider.UninstallCallIds;
            Assert.Equal(new[] { operationId, operationId }, operationIds);
            if (kind == "revocation")
            {
                Assert.True(state.GetProperty("generationPayloadPresent").GetBoolean());
            }
        }
        else
        {
            Assert.False(recovery.GetProperty("accepted").GetBoolean());
            Assert.Equal("offboarding_outcome_unknown", recovery.GetProperty("safeErrorCode").GetString());
            Assert.Equal("UnknownOutcome", operation.GetProperty("status").GetString());
            Assert.Equal(1, kind == "revocation" ? provider.RevocationCalls : provider.UninstallCalls);
            Assert.Equal(1, kind == "revocation" ? provider.RevocationEffects : provider.UninstallEffects);
            if (kind == "revocation")
            {
                Assert.True(state.GetProperty("generationAvailable").GetBoolean());
                Assert.True(state.GetProperty("generationPayloadPresent").GetBoolean());
            }
        }

        if (kind == "revocation" && stableOperationId)
        {
            var cleanup = await _workers.RunAsync(["cleanup", connectionId, ProcessTestEnvironment.TenantId, ProcessTestEnvironment.EnvironmentId, generationId], recoveryEnvironment);
            Assert.True(cleanup.ReadResult().GetProperty("succeeded").GetBoolean());
            var afterCleanup = (await _workers.RunAsync(["inspect", connectionId, generationId], recoveryEnvironment)).ReadResult();
            Assert.False(afterCleanup.GetProperty("generationAvailable").GetBoolean());
            Assert.True(afterCleanup.GetProperty("generationRecordPresent").GetBoolean());
            Assert.Equal("Deleted", afterCleanup.GetProperty("generationStatus").GetString());
            Assert.True(afterCleanup.GetProperty("generationOwnershipPreserved").GetBoolean());
            Assert.False(afterCleanup.GetProperty("generationPayloadPresent").GetBoolean());
            Assert.False(afterCleanup.GetProperty("generationPlaintextValuePresent").GetBoolean());
            Assert.Equal(JsonValueKind.Null, afterCleanup.GetProperty("currentGenerationId").ValueKind);
        }
        else if (kind == "revocation")
        {
            var cleanup = await _workers.RunAsync(["cleanup", connectionId, ProcessTestEnvironment.TenantId, ProcessTestEnvironment.EnvironmentId, generationId], recoveryEnvironment);
            Assert.False(cleanup.ReadResult().GetProperty("succeeded").GetBoolean());
            Assert.Equal("generation_in_use", cleanup.ReadResult().GetProperty("safeErrorCode").GetString());
            Assert.True(state.GetProperty("generationAvailable").GetBoolean());
        }

        AssertSafeOutput(queued, ProcessTestEnvironment.AccessMarker, ProcessTestEnvironment.RefreshMarker);
        AssertSafeOutput(crashOutput, ProcessTestEnvironment.AccessMarker, ProcessTestEnvironment.RefreshMarker);
        AssertSafeOutput(recoveryResult, ProcessTestEnvironment.AccessMarker, ProcessTestEnvironment.RefreshMarker);
        AssertSafeOutput(inspect, ProcessTestEnvironment.AccessMarker, ProcessTestEnvironment.RefreshMarker);
    }

    [Fact]
    public async Task RevocationQueueAndGenerationCleanup_LinearizeAcrossTwoPostgreSqlProcesses()
    {
        await fixture.ResetSchemaAsync();
        await using var provider = await SyntheticOAuthServer.StartAsync();
        provider.IssueRefreshToken(ProcessTestEnvironment.RefreshMarker);
        var environment = await MigrateAsync(provider);
        var (connectionId, generationId, _) = await ConnectAsync(environment);
        Assert.Equal(0, (await _workers.RunAsync(["disconnect", connectionId], environment)).ExitCode);

        var barrierDirectory = Path.Join(Path.GetTempPath(), $"elsa-connection-race-{Guid.NewGuid():N}");
        Directory.CreateDirectory(barrierDirectory);
        try
        {
            var queueEnvironment = ProcessTestEnvironment.With(
                environment,
                ("ELSA_TEST_BARRIER_METHOD", "TryQueueOffboardingOperationAsync"),
                ("ELSA_TEST_BARRIER_PARTICIPANT", "queue"),
                ("ELSA_TEST_BARRIER_DIRECTORY", barrierDirectory));
            var cleanupEnvironment = ProcessTestEnvironment.With(
                environment,
                ("ELSA_TEST_BARRIER_METHOD", "TryClaimGenerationCleanupAsync"),
                ("ELSA_TEST_BARRIER_PARTICIPANT", "cleanup"),
                ("ELSA_TEST_BARRIER_DIRECTORY", barrierDirectory));
            await using var queueWorker = _workers.Start(
                ["request-revocation", connectionId, ProcessTestEnvironment.TenantId, ProcessTestEnvironment.EnvironmentId, generationId],
                queueEnvironment);
            await using var cleanupWorker = _workers.Start(
                ["cleanup", connectionId, ProcessTestEnvironment.TenantId, ProcessTestEnvironment.EnvironmentId, generationId],
                cleanupEnvironment);

            await queueWorker.WaitForLineAsync("BARRIER_READY:queue", TimeSpan.FromSeconds(30));
            await cleanupWorker.WaitForLineAsync("BARRIER_READY:cleanup", TimeSpan.FromSeconds(30));
            await File.WriteAllTextAsync(Path.Join(barrierDirectory, "go"), "go");
            var results = await Task.WhenAll(queueWorker.CompleteAsync(), cleanupWorker.CompleteAsync());
            var queued = results[0].ReadResult();
            var cleaned = results[1].ReadResult();
            var inspect = await _workers.RunAsync(["inspect", connectionId, generationId], environment);
            var state = inspect.ReadResult();
            var isQueued = queued.GetProperty("accepted").GetBoolean();
            var isCleaned = cleaned.GetProperty("succeeded").GetBoolean();

            Assert.NotEqual(isQueued, isCleaned);
            if (isQueued)
            {
                Assert.False(isCleaned);
                Assert.True(state.GetProperty("generationAvailable").GetBoolean());
                Assert.True(state.GetProperty("generationPayloadPresent").GetBoolean());
                Assert.Equal(generationId, state.GetProperty("currentGenerationId").GetString());
                var revocation = Assert.Single(
                    state.GetProperty("operations").EnumerateArray(),
                    operation => operation.GetProperty("kind").GetString() == "TokenPairRevocation");
                Assert.Equal("Pending", revocation.GetProperty("status").GetString());
            }
            else
            {
                Assert.True(isCleaned);
                Assert.False(state.GetProperty("generationAvailable").GetBoolean());
                Assert.True(state.GetProperty("generationRecordPresent").GetBoolean());
                Assert.Equal("Deleted", state.GetProperty("generationStatus").GetString());
                Assert.True(state.GetProperty("generationOwnershipPreserved").GetBoolean());
                Assert.False(state.GetProperty("generationPayloadPresent").GetBoolean());
                Assert.False(state.GetProperty("generationPlaintextValuePresent").GetBoolean());
                Assert.Equal("Deleted", state.GetProperty("cleanupStatus").GetString());
                Assert.Equal(JsonValueKind.Null, state.GetProperty("currentGenerationId").ValueKind);
                Assert.DoesNotContain(
                    state.GetProperty("operations").EnumerateArray(),
                    operation => operation.GetProperty("kind").GetString() == "TokenPairRevocation");
            }

            AssertSafeOutput(results[0], ProcessTestEnvironment.AccessMarker, ProcessTestEnvironment.RefreshMarker);
            AssertSafeOutput(results[1], ProcessTestEnvironment.AccessMarker, ProcessTestEnvironment.RefreshMarker);
            AssertSafeOutput(inspect, ProcessTestEnvironment.AccessMarker, ProcessTestEnvironment.RefreshMarker);
        }
        finally
        {
            if (Directory.Exists(barrierDirectory))
            {
                Directory.Delete(barrierDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task WorkflowRestart_ReloadsPersistedLogicalReferenceAndTrustedTenantAndRejectsScopeChanges()
    {
        await fixture.ResetSchemaAsync();
        await using var provider = await SyntheticOAuthServer.StartAsync();
        provider.IssueRefreshToken(ProcessTestEnvironment.RefreshMarker);
        var environment = await MigrateAsync(provider);
        var (connectionId, _, _) = await ConnectAsync(environment);
        const string logicalBindingId = "payments";
        const string workflowInstanceId = "workflow-process-restart";

        var binding = await _workers.RunAsync(["bind", logicalBindingId, connectionId], environment);
        Assert.True(binding.ReadResult().GetProperty("succeeded").GetBoolean());
        Assert.True((await _workers.RunAsync(["save-workflow-state", workflowInstanceId, logicalBindingId], environment))
            .ReadResult().GetProperty("stored").GetBoolean());
        var persistedState = await _workers.RunAsync(["inspect-workflow-state", workflowInstanceId], environment);
        Assert.Equal(logicalBindingId, persistedState.ReadResult().GetProperty("logicalBindingId").GetString());
        Assert.DoesNotContain(connectionId, persistedState.CapturedOutput, StringComparison.Ordinal);
        Assert.DoesNotContain("generation", persistedState.CapturedOutput, StringComparison.OrdinalIgnoreCase);
        AssertSafeOutput(persistedState, ProcessTestEnvironment.AccessMarker, ProcessTestEnvironment.RefreshMarker);

        var restartEnvironment = ProcessTestEnvironment.With(environment, ("ELSA_TEST_EXPECTED_ACCESS_TOKEN", ProcessTestEnvironment.AccessMarker));
        var restarted = await _workers.RunAsync(["restart-workflow", workflowInstanceId], restartEnvironment);
        var restartedResult = restarted.ReadResult();
        Assert.True(restartedResult.GetProperty("resolved").GetBoolean());
        Assert.Equal(ProcessTestEnvironment.TenantId, restartedResult.GetProperty("observedTenant").GetString());
        Assert.Equal(string.Empty, restartedResult.GetProperty("ambientTenantAfterRestart").GetString());
        Assert.Equal(0, provider.RefreshCalls);

        var deniedWorkerEnvironment = ProcessTestEnvironment.With(
            environment,
            ("ELSA_TEST_PERMISSIONS", "connections.use"));
        var deniedRefresh = await _workers.RunAsync(["refresh", connectionId], deniedWorkerEnvironment);
        Assert.False(deniedRefresh.ReadResult().GetProperty("succeeded").GetBoolean());
        Assert.Equal("connection_unavailable", deniedRefresh.ReadResult().GetProperty("safeErrorCode").GetString());
        Assert.Equal(0, provider.RefreshCalls);

        var wrongTenant = await _workers.RunAsync(
            ["refresh", connectionId, "tenant-other", ProcessTestEnvironment.EnvironmentId],
            environment);
        Assert.False(wrongTenant.ReadResult().GetProperty("succeeded").GetBoolean());
        Assert.Equal("connection_unavailable", wrongTenant.ReadResult().GetProperty("safeErrorCode").GetString());
        var wrongEnvironment = await _workers.RunAsync(
            ["refresh", connectionId, ProcessTestEnvironment.TenantId, "staging"],
            environment);
        Assert.False(wrongEnvironment.ReadResult().GetProperty("succeeded").GetBoolean());
        Assert.Equal("connection_unavailable", wrongEnvironment.ReadResult().GetProperty("safeErrorCode").GetString());
        Assert.Equal(0, provider.RefreshCalls);

        var wrongHost = ProcessTestEnvironment.With(
            restartEnvironment,
            ("ELSA_TEST_ENVIRONMENT_ID", "staging"));
        var wrongHostRestart = await _workers.RunAsync(["restart-workflow", workflowInstanceId], wrongHost);
        Assert.False(wrongHostRestart.ReadResult().GetProperty("resolved").GetBoolean());
        Assert.Equal(ProcessTestEnvironment.TenantId, wrongHostRestart.ReadResult().GetProperty("observedTenant").GetString());
        Assert.Equal(0, provider.RefreshCalls);

        AssertSafeOutput(binding, ProcessTestEnvironment.AccessMarker, ProcessTestEnvironment.RefreshMarker);
        AssertSafeOutput(persistedState, ProcessTestEnvironment.AccessMarker, ProcessTestEnvironment.RefreshMarker);
        AssertSafeOutput(restarted, ProcessTestEnvironment.AccessMarker, ProcessTestEnvironment.RefreshMarker);
        AssertSafeOutput(deniedRefresh, ProcessTestEnvironment.AccessMarker, ProcessTestEnvironment.RefreshMarker);
        AssertSafeOutput(wrongTenant, ProcessTestEnvironment.AccessMarker, ProcessTestEnvironment.RefreshMarker);
        AssertSafeOutput(wrongEnvironment, ProcessTestEnvironment.AccessMarker, ProcessTestEnvironment.RefreshMarker);
        AssertSafeOutput(wrongHostRestart, ProcessTestEnvironment.AccessMarker, ProcessTestEnvironment.RefreshMarker);
    }

    private async Task<Dictionary<string, string>> MigrateAsync(SyntheticOAuthServer provider)
    {
        var environment = ProcessTestEnvironment.Create(fixture.ConnectionString, provider.Address);
        var migration = await _workers.RunAsync(["migrate"], environment);
        Assert.True(migration.ExitCode == 0, migration.CapturedOutput);
        Assert.True(migration.ReadResult().GetProperty("migrated").GetBoolean());
        return environment;
    }

    private async Task<(string ConnectionId, string GenerationId, ProcessRunResult Result)> ConnectAsync(
        IReadOnlyDictionary<string, string> environment)
    {
        var result = await _workers.RunAsync(["connect"], environment);
        Assert.Equal(0, result.ExitCode);
        var json = result.ReadResult();
        Assert.True(json.GetProperty("succeeded").GetBoolean());
        var connectionId = json.GetProperty("connectionId").GetString();
        var generationId = json.GetProperty("connection").GetProperty("generationId").GetString();
        Assert.False(string.IsNullOrWhiteSpace(connectionId));
        Assert.False(string.IsNullOrWhiteSpace(generationId));
        return (connectionId!, generationId!, result);
    }

    private static void AssertSafeOutput(ProcessRunResult result, params string[] markers)
    {
        foreach (var marker in markers
                     .Append(ProcessTestEnvironment.RotatedAccessTokenPrefix)
                     .Append(ProcessTestEnvironment.RotatedRefreshTokenPrefix)
                     .Append(Convert.ToBase64String(ProcessTestEnvironment.EncryptionKey)))
        {
            Assert.DoesNotContain(marker, result.CapturedOutput, StringComparison.Ordinal);
        }
    }
}
