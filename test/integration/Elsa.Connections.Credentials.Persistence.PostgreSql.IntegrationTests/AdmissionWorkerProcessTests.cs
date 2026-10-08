using System.Globalization;
using System.Text.Json;
using Elsa.Persistence.EFCore.Modules.Management;
using Elsa.Workflows.Admission;
using Microsoft.Extensions.DependencyInjection;
using Elsa.Workflows.Admission.WorkerProcess;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Connections.Credentials.Persistence.PostgreSql.IntegrationTests;

[Collection("Connections PostgreSQL")]
public sealed class AdmissionWorkerProcessTests(PostgreSqlConnectionsFixture fixture)
{
    private static WorkerProcessRunner Runner() => new(typeof(AdmissionWorkerHost).Assembly.Location);

    [Fact]
    public async Task ConcurrentDuplicateAdmissionsCommitOneAllocation()
    {
        await using var services = await AdmissionTestLedger.CreateAsync(fixture);
        using var gate = new AdmissionProcessGate();
        var environment = WorkerEnvironment(gate.Path);
        await using var first = Runner().Start(["admit", "event-race"], environment);
        await using var second = Runner().Start(["admit", "event-race"], environment);
        await Task.WhenAll(first.WaitForLineAsync("BARRIER_READY"), second.WaitForLineAsync("BARRIER_READY"));
        await gate.ReleaseAsync();
        var results = await Task.WhenAll(first.CompleteAsync(), second.CompleteAsync());
        AssertDistinctSuccessfulProcesses(results);
        var outcomes = results.Select(x => Result(x).GetProperty("outcome").GetString()).OrderBy(x => x).ToArray();
        Assert.Equal(new[] { "Committed", "Duplicate" }, outcomes);
        Assert.Equal(Result(results[0]).GetProperty("admissionId").GetString(), Result(results[1]).GetProperty("admissionId").GetString());
        await using var db = await AdmissionTestLedger.ContextAsync(services);
        Assert.Equal(1, await db.Admissions.CountAsync());
        var subscription = (await AdmissionTestLedger.Store(services).FindSubscriptionAsync(AdmissionWorkerHost.Configuration().Id))!;
        Assert.Equal(1, subscription.ActiveReservations);
        Assert.Equal(1, subscription.RetainedRecords);
        await ObserveAsync("provider-duplicate", nameof(ConcurrentDuplicateAdmissionsCommitOneAllocation), results,
            new() { ["admissionCount"] = 1, ["activeReservations"] = subscription.ActiveReservations, ["retainedRecords"] = subscription.RetainedRecords });
    }

    [Fact]
    public async Task ConcurrentCapacityReservationsRejectOneWithoutAcknowledgement()
    {
        await using var services = await AdmissionTestLedger.CreateAsync(fixture, AdmissionWorkerHost.Configuration(1, 1));
        using var gate = new AdmissionProcessGate();
        await using var first = Runner().Start(["admit", "event-a"], WorkerEnvironment(gate.Path));
        await using var second = Runner().Start(["admit", "event-b"], WorkerEnvironment(gate.Path));
        await Task.WhenAll(first.WaitForLineAsync("BARRIER_READY"), second.WaitForLineAsync("BARRIER_READY"));
        await gate.ReleaseAsync();
        var results = await Task.WhenAll(first.CompleteAsync(), second.CompleteAsync());
        AssertDistinctSuccessfulProcesses(results);
        Assert.Equal(1, results.Count(x => Result(x).GetProperty("acknowledgementEligible").GetBoolean()));
        Assert.Equal(1, results.Count(x => Result(x).GetProperty("outcome").GetString() == "CapacityExceeded"));
        await using var db = await AdmissionTestLedger.ContextAsync(services);
        Assert.Equal(1, await db.Admissions.CountAsync());
        await ObserveAsync("provider-capacity", nameof(ConcurrentCapacityReservationsRejectOneWithoutAcknowledgement), results,
            new() { ["admissionCount"] = 1, ["acknowledgementEligibleCount"] = 1, ["capacityExceededCount"] = 1 });
    }

    [Fact]
    public async Task CrashBeforeCommitLeavesNoAdmissionAndRestartAdmits()
    {
        await using var services = await AdmissionTestLedger.CreateAsync(fixture);
        using var gate = new AdmissionProcessGate();
        await using var crashed = Runner().Start(["admit", "event-crash"], WorkerEnvironment(commitGate: gate.Path, boundary: "before-commit"));
        await crashed.WaitForLineAsync("BOUNDARY:before-commit");
        await using (var db = await AdmissionTestLedger.ContextAsync(services))
        {
            Assert.Equal(0, await db.Admissions.CountAsync());
        }
        var killed = await crashed.TerminateAsync();
        Assert.NotEqual(0, killed.ExitCode);
        var restarted = await Runner().RunAsync(["admit", "event-crash"], WorkerEnvironment());
        Assert.Equal(0, restarted.ExitCode);
        Assert.Equal("Committed", Result(restarted).GetProperty("outcome").GetString());
        Assert.NotEqual(killed.ProcessId, restarted.ProcessId);
        await using var final = await AdmissionTestLedger.ContextAsync(services);
        Assert.Equal(1, await final.Admissions.CountAsync());
        await ObserveAsync("provider-before-commit", nameof(CrashBeforeCommitLeavesNoAdmissionAndRestartAdmits), [killed, restarted],
            new() { ["admissionsBeforeKill"] = 0, ["admissionsAfterRestart"] = 1, ["restartCommitted"] = true }, restarted: true);
    }

    [Fact]
    public async Task CrashAfterCommitBeforeAcknowledgementRedeliveryUsesSameAllocation()
    {
        await using var services = await AdmissionTestLedger.CreateAsync(fixture);
        using var gate = new AdmissionProcessGate();
        await using var crashed = Runner().Start(["admit", "event-crash"], WorkerEnvironment(commitGate: gate.Path, boundary: "after-commit"));
        await crashed.WaitForLineAsync("BOUNDARY:after-commit");
        string allocated;
        await using (var db = await AdmissionTestLedger.ContextAsync(services))
        {
            allocated = (await db.Admissions.SingleAsync()).Id;
        }
        var killed = await crashed.TerminateAsync();
        Assert.DoesNotContain(killed.StandardOutput, x => x.StartsWith("RESULT:", StringComparison.Ordinal));
        var restarted = await Runner().RunAsync(["admit", "event-crash"], WorkerEnvironment());
        Assert.Equal(0, restarted.ExitCode);
        Assert.Equal("Duplicate", Result(restarted).GetProperty("outcome").GetString());
        Assert.Equal(allocated, Result(restarted).GetProperty("admissionId").GetString());
        await using var final = await AdmissionTestLedger.ContextAsync(services);
        Assert.Equal(1, await final.Admissions.CountAsync());
        await ObserveAsync("provider-after-commit", nameof(CrashAfterCommitBeforeAcknowledgementRedeliveryUsesSameAllocation), [killed, restarted],
            new() { ["admissionCount"] = 1, ["acknowledgementObservedBeforeKill"] = false, ["sameAllocationAfterRestart"] = true }, restarted: true);
    }

    [Fact]
    public async Task UnknownCommitResponseThrowsAndReadbackDoesNotRepeatAuthorization()
    {
        await using var services = await AdmissionTestLedger.CreateAsync(fixture);
        var store = AdmissionTestLedger.Store(services);
        var prepared = await AdmissionTestLedger.PrepareAsync(store);
        using var gate = new AdmissionProcessGate();
        await using var uncertain = Runner().Start(AuthorizeArguments(prepared), WorkerEnvironment(commitGate: gate.Path, boundary: "unknown-commit"));
        await uncertain.WaitForLineAsync("BOUNDARY:unknown-commit");
        var visible = (await store.FindAsync(prepared.Id))!;
        Assert.Equal(AdmissionState.StartAuthorized, visible.State);
        Assert.True(visible.AuthorityOutstanding);
        await gate.ReleaseAsync();
        var failed = await uncertain.CompleteAsync();
        Assert.Equal(2, failed.ExitCode);
        Assert.DoesNotContain(failed.StandardOutput, x => x.StartsWith("RESULT:", StringComparison.Ordinal));
        var duplicate = await Runner().RunAsync(AuthorizeArguments(prepared), WorkerEnvironment());
        Assert.Equal(0, duplicate.ExitCode);
        Assert.False(Result(duplicate).GetProperty("changed").GetBoolean());
        var preserved = (await store.FindAsync(prepared.Id))!;
        Assert.Equal(visible.Revision, preserved.Revision);
        Assert.Equal(AdmissionState.StartAuthorized, preserved.State);
        await ObserveAsync("provider-unknown-permit", nameof(UnknownCommitResponseThrowsAndReadbackDoesNotRepeatAuthorization), [failed, duplicate],
            new() { ["state"] = preserved.State.ToString(), ["authorityOutstanding"] = preserved.AuthorityOutstanding, ["revisionPreserved"] = true, ["duplicateAuthorizationChanged"] = false }, restarted: true);
    }

    [Fact]
    public async Task WithdrawalBeforeAuthorizationDeniesAndNewEpochDoesNotRevive()
    {
        await using var services = await AdmissionTestLedger.CreateAsync(fixture);
        var store = AdmissionTestLedger.Store(services);
        var prepared = await AdmissionTestLedger.PrepareAsync(store);
        var subscription = (await store.FindSubscriptionAsync(prepared.SubscriptionId))!;
        var withdrawal = await Runner().RunAsync(["withdraw", subscription.Revision.ToString(CultureInfo.InvariantCulture)], WorkerEnvironment());
        Assert.Equal(0, withdrawal.ExitCode);
        Assert.True(Result(withdrawal).GetProperty("changed").GetBoolean());
        var denied = await Runner().RunAsync(AuthorizeArguments(prepared), WorkerEnvironment());
        Assert.Equal(0, denied.ExitCode);
        Assert.False(Result(denied).GetProperty("changed").GetBoolean());
        var withdrawn = (await store.FindSubscriptionAsync(prepared.SubscriptionId))!;
        var reactivated = (await store.ActivateAsync(withdrawn.Id, withdrawn.Revision, AdmissionWorkerHost.Now))!;
        Assert.True(reactivated.ActivationEpoch > prepared.ActivationEpoch);
        Assert.Null(await store.AuthorizeStartAsync(prepared.Id, prepared.Revision, prepared.AttemptId!));
        await ObserveAsync("provider-withdraw-first", nameof(WithdrawalBeforeAuthorizationDeniesAndNewEpochDoesNotRevive), [withdrawal, denied],
            new() { ["staleAuthorizationDenied"] = true, ["epochAdvanced"] = true, ["authorityOutstanding"] = false });
    }

    [Fact]
    public async Task AuthorizationBeforeWithdrawalRetainsOutstandingHistoricalAuthority()
    {
        await using var services = await AdmissionTestLedger.CreateAsync(fixture);
        var store = AdmissionTestLedger.Store(services);
        var prepared = await AdmissionTestLedger.PrepareAsync(store);
        using var gate = new AdmissionProcessGate();
        await using var authorizing = Runner().Start(AuthorizeArguments(prepared), WorkerEnvironment(commitGate: gate.Path, boundary: "after-commit"));
        await authorizing.WaitForLineAsync("BOUNDARY:after-commit");
        var subscription = (await store.FindSubscriptionAsync(prepared.SubscriptionId))!;
        var withdrawn = await Runner().RunAsync(["withdraw", subscription.Revision.ToString(CultureInfo.InvariantCulture)], WorkerEnvironment());
        Assert.Equal(0, withdrawn.ExitCode);
        Assert.True(Result(withdrawn).GetProperty("changed").GetBoolean());
        var killed = await authorizing.TerminateAsync();
        var historical = (await store.FindAsync(prepared.Id))!;
        Assert.Equal(AdmissionState.StartAuthorized, historical.State);
        Assert.True(historical.AuthorityOutstanding);
        Assert.Null(await store.ResolveAsync(historical.Id, historical.Revision, AdmissionTerminalDisposition.Resolved, "audit:still-delayed", false, true, AdmissionWorkerHost.Now.AddYears(1)));
        Assert.False(await store.CleanupAsync(historical.Id, historical.Revision, "fixture-cleanup-authority", AdmissionWorkerHost.Now.AddYears(1)));
        await ObserveAsync("provider-permit-first", nameof(AuthorizationBeforeWithdrawalRetainsOutstandingHistoricalAuthority), [killed, withdrawn],
            new() { ["state"] = historical.State.ToString(), ["authorityOutstanding"] = true, ["unsafeResolutionDenied"] = true, ["cleanupDenied"] = true });
    }

    [Fact]
    public async Task ConcurrentTerminalCleanupReleasesRetainedReservationOnce()
    {
        await using var services = await AdmissionTestLedger.CreateAsync(fixture, AdmissionWorkerHost.Configuration(1, 1));
        var store = AdmissionTestLedger.Store(services);
        var admitted = await store.AdmitAsync(AdmissionWorkerHost.Event(), AdmissionWorkerHost.Now);
        var terminal = (await store.ResolveAsync(admitted.AdmissionId!, admitted.Revision!.Value, AdmissionTerminalDisposition.SuppressedBeforeStart,
            "audit:fixture-suppression", false, true, AdmissionWorkerHost.Now))!;
        using var gate = new AdmissionProcessGate();
        var arguments = new[] { "cleanup", terminal.Id, terminal.Revision.ToString(CultureInfo.InvariantCulture), AdmissionWorkerHost.Now.AddDays(3).ToString("O") };
        await using var first = Runner().Start(arguments, WorkerEnvironment(gate.Path));
        await using var second = Runner().Start(arguments, WorkerEnvironment(gate.Path));
        await Task.WhenAll(first.WaitForLineAsync("BARRIER_READY"), second.WaitForLineAsync("BARRIER_READY"));
        await gate.ReleaseAsync();
        var results = await Task.WhenAll(first.CompleteAsync(), second.CompleteAsync());
        AssertDistinctSuccessfulProcesses(results);
        Assert.Equal(1, results.Count(x => Result(x).GetProperty("cleaned").GetBoolean()));
        var subscription = (await store.FindSubscriptionAsync(terminal.SubscriptionId))!;
        Assert.Equal(0, subscription.ActiveReservations);
        Assert.Equal(0, subscription.RetainedRecords);
        Assert.Null(await store.FindAsync(terminal.Id));
        await ObserveAsync("provider-cleanup-race", nameof(ConcurrentTerminalCleanupReleasesRetainedReservationOnce), results,
            new() { ["cleanupSuccessCount"] = 1, ["activeReservations"] = 0, ["retainedRecords"] = 0, ["recordDeleted"] = true });
    }

    [Fact]
    public async Task CompetingBootstrapSubscriptionsSerializeOneLogicalDefinitionInsert()
    {
        await fixture.ResetSchemaAsync();
        await using var services = AdmissionWorkerHost.CreateServices(fixture.ConnectionString, includeManagement: true);
        await using (var db = await services.GetRequiredService<IDbContextFactory<ManagementElsaDbContext>>().CreateDbContextAsync())
        {
            await db.Database.MigrateAsync();
        }
        await AdmissionWorkerHost.MigrateAsync(services);
        using var gate = new AdmissionProcessGate();
        var firstEnvironment = WorkerEnvironment();
        firstEnvironment["ELSA_ADMISSION_BOOTSTRAP_GATE"] = gate.Path;
        await using var first = Runner().Start(["bootstrap-insert", "subscription-first"], firstEnvironment);
        await first.WaitForLineAsync("BOUNDARY:bootstrap-exclusive");
        await using var second = Runner().Start(["bootstrap-insert", "subscription-second"], WorkerEnvironment());
        await second.WaitForLineAsync("BOUNDARY:bootstrap-acquiring");
        await gate.ReleaseAsync();
        var results = await Task.WhenAll(first.CompleteAsync(), second.CompleteAsync());
        AssertDistinctSuccessfulProcesses(results);
        Assert.Equal("Inserted", Result(results[0]).GetProperty("outcome").GetString());
        Assert.Equal("ExistingMatch", Result(results[1]).GetProperty("outcome").GetString());
        await using var final = await services.GetRequiredService<IDbContextFactory<ManagementElsaDbContext>>().CreateDbContextAsync();
        Assert.Equal(1, await final.WorkflowDefinitions.CountAsync());
        Assert.False((await final.WorkflowDefinitions.SingleAsync()).IsPublished);
        await ObserveAsync("provider-bootstrap-race", nameof(CompetingBootstrapSubscriptionsSerializeOneLogicalDefinitionInsert), results,
            new() { ["definitionCount"] = 1, ["insertedCount"] = 1, ["existingMatchCount"] = 1, ["published"] = false });
    }

    private Task ObserveAsync(string caseId, string method, IReadOnlyList<ProcessRunResult> results, Dictionary<string, object> facts, bool restarted = false)
    {
        Assert.Equal(results.Count, results.Select(x => x.ProcessId).Distinct().Count());
        foreach (var result in results)
        {
            Assert.DoesNotContain(ProcessTestEnvironment.AccessMarker, result.CapturedOutput);
            Assert.DoesNotContain(ProcessTestEnvironment.RefreshMarker, result.CapturedOutput);
        }
        return AdmissionProofObservation.WriteAsync(fixture, caseId, GetType().FullName + "." + method, "default",
            results.Select((result, index) => (result, index == 0 ? "admission-primary" : "admission-competitor", restarted ? index + 1 : 1)).ToArray(),
            new Dictionary<string, bool> { ["durablePredicatesVerified"] = true, ["independentProcessesObserved"] = true, ["safeOutputVerified"] = true }, facts);
    }

    private Dictionary<string, string> WorkerEnvironment(string? startGate = null, string? commitGate = null, string? boundary = null)
    {
        var environment = new Dictionary<string, string> { ["ELSA_TEST_CONNECTION_STRING"] = fixture.ConnectionString };
        if (startGate != null)
        {
            environment["ELSA_ADMISSION_START_GATE"] = startGate;
        }
        if (commitGate != null)
        {
            environment["ELSA_ADMISSION_COMMIT_GATE"] = commitGate;
        }
        if (boundary != null)
        {
            environment["ELSA_ADMISSION_COMMIT_BOUNDARY"] = boundary;
        }
        return environment;
    }

    private static string[] AuthorizeArguments(AdmissionRecord prepared) => ["authorize", prepared.Id, prepared.Revision.ToString(CultureInfo.InvariantCulture), prepared.AttemptId!];
    private static JsonElement Result(ProcessRunResult process) => process.ReadResult().GetProperty("result");

    private static void AssertDistinctSuccessfulProcesses(IReadOnlyList<ProcessRunResult> results)
    {
        Assert.Equal(results.Count, results.Select(x => x.ProcessId).Distinct().Count());
        Assert.All(results, result => Assert.Equal(0, result.ExitCode));
    }
}

internal sealed class AdmissionProcessGate : IDisposable
{
    private readonly string _directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "elsa-admission-" + Guid.NewGuid().ToString("N"));
    public string Path => System.IO.Path.Combine(_directory, "release");
    public AdmissionProcessGate() => Directory.CreateDirectory(_directory);
    public Task ReleaseAsync() => File.WriteAllTextAsync(Path, "release");
    public void Dispose() => Directory.Delete(_directory, recursive: true);
}
