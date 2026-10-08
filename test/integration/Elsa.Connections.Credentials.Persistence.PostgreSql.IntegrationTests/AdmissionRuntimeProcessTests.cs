using Elsa.Workflows.Admission;
using Elsa.Workflows.Admission.WorkerProcess;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Elsa.Connections.Credentials.Persistence.PostgreSql.IntegrationTests;

[Collection("Connections PostgreSQL")]
public sealed class AdmissionRuntimeProcessTests(PostgreSqlConnectionsFixture fixture)
{
    [Theory]
    [InlineData("runtime-crash-creation-claim", "CreationClaimed", 0, 0, false)]
    [InlineData("runtime-crash-before-insert", "BeforeInstanceInsert", 0, 0, false)]
    [InlineData("runtime-crash-insert-notified", "InstanceInsertedAndNotified", 1, 0, false)]
    [InlineData("runtime-crash-start-prepared", "StartPrepared", 1, 0, false)]
    [InlineData("runtime-crash-start-authorized", "StartAuthorized", 1, 0, false)]
    [InlineData("runtime-crash-before-entry", "BeforeRunnerEntry", 1, 0, false)]
    [InlineData("runtime-crash-authority-consumed", "AuthorityConsumed", 1, 0, false)]
    [InlineData("runtime-crash-effect", "ActivityEffect", 1, 1, false)]
    [InlineData("runtime-crash-final-commit", "FinalCommit", 1, 1, false)]
    [InlineData("runtime-crash-trailing-write", "TrailingWrite", 1, 1, false)]
    [InlineData("runtime-crash-ownership-unwound", "OwnershipUnwound", 1, 1, false)]
    [InlineData("runtime-crash-checkpoint-recorded", "CheckpointRecorded", 1, 1, true)]
    public async Task SequentialRealRuntimeRestartNeverReplaysAmbiguousCreationAuthorityOrEffects(
        string caseId, string boundary, int expectedInstances, int expectedEffects, bool terminalAlreadyRecorded)
    {
        var admissionId = await PrepareInactiveExecutionHostAsync();
        // This provider has ledger services only: no second runtime sharing workflow stores.
        await using var controller = AdmissionWorkerHost.CreateServices(fixture.ConnectionString);
        var store = controller.GetRequiredService<IAdmissionStore>();
        var probe = new AdmissionRuntimeProbe(fixture.ConnectionString);
        using var gate = new AdmissionProcessGate();
        var runner = new WorkerProcessRunner(typeof(AdmissionWorkerHost).Assembly.Location);
        await using var running = runner.Start(["runtime-execute", admissionId], EnvironmentFor(boundary, gate.Path));
        await running.WaitForLineAsync("BOUNDARY:" + boundary);
        var before = (await store.FindAsync(admissionId))!;
        Assert.NotNull(before.WorkflowInstanceId);
        Assert.Equal(expectedInstances, await CountInstancesAsync(before.WorkflowInstanceId));
        Assert.Equal(expectedEffects, await probe.ReadDurableCountAsync("activityEffects"));
        if (!terminalAlreadyRecorded)
        {
            Assert.NotEqual(AdmissionState.Terminal, before.State);
        }
        // A distinct controller OS process observes the real durable row while the only
        // executing host is paused. It has no execution services/capability.
        var observed = await runner.RunAsync(["inspect", admissionId], EnvironmentFor());
        Assert.Equal(0, observed.ExitCode);
        Assert.Equal(before.State.ToString(), observed.ReadResult().GetProperty("result").GetProperty("state").GetString());
        var killed = await running.TerminateAsync();
        Assert.NotEqual(0, killed.ExitCode);
        Assert.DoesNotContain(killed.StandardOutput, value => value.StartsWith("RESULT:", StringComparison.Ordinal));
        // Only after the original process has exited can the replacement runtime recover.
        var restarted = await runner.RunAsync(["runtime-recover", admissionId], EnvironmentFor());
        Assert.Equal(0, restarted.ExitCode);
        var result = restarted.ReadResult().GetProperty("result");
        Assert.False(result.GetProperty("replayed").GetBoolean());
        var after = (await store.FindAsync(admissionId))!;
        Assert.Equal(terminalAlreadyRecorded ? AdmissionState.Terminal : AdmissionState.RecoveryRequired, after.State);
        Assert.Equal(expectedEffects, await probe.ReadDurableCountAsync("activityEffects"));
        Assert.Equal(expectedInstances, await CountInstancesAsync(before.WorkflowInstanceId));
        Assert.Equal(0, await probe.ReadDurableCountAsync("activityResumes"));
        Assert.Equal(3, new[] { killed.ProcessId, observed.ProcessId, restarted.ProcessId }.Distinct().Count());
        await AdmissionProofObservation.WriteAsync(fixture, caseId, GetType().FullName + "." + nameof(SequentialRealRuntimeRestartNeverReplaysAmbiguousCreationAuthorityOrEffects), caseId,
            [(killed, "admission-runtime", 1), (observed, "admission-controller", 1), (restarted, "admission-runtime", 2)],
            new Dictionary<string, bool> { ["durablePredicatesVerified"] = true, ["independentProcessesObserved"] = true, ["noReplayVerified"] = true },
            new Dictionary<string, object>
            {
                ["instanceCount"] = expectedInstances, ["activityEffects"] = expectedEffects, ["activityResumes"] = 0,
                ["terminalAlreadyRecorded"] = terminalAlreadyRecorded, ["replayed"] = false, ["state"] = after.State.ToString()
            });
    }

    private async Task<string> PrepareInactiveExecutionHostAsync()
    {
        await fixture.ResetSchemaAsync();
        var probe = new AdmissionRuntimeProbe(fixture.ConnectionString);
        await using var bootstrap = AdmissionRuntimeHost.CreateServices(fixture.ConnectionString, probe);
        using var tenant = AdmissionRuntimeHost.EnterTenant(bootstrap);
        await AdmissionRuntimeHost.MigrateAsync(bootstrap);
        await AdmissionRuntimeHost.BootstrapAsync(bootstrap);
        var admitted = await bootstrap.GetRequiredService<AdmissionExecutionService>().AdmitAsync(AdmissionWorkerHost.Event());
        Assert.Equal(AdmissionOutcome.Committed, admitted.Outcome);
        return admitted.AdmissionId!;
        // Disposal happens before an execution worker is started; never two execution hosts.
    }

    private Dictionary<string, string> EnvironmentFor(string? boundary = null, string? gate = null)
    {
        var environment = new Dictionary<string, string> { ["ELSA_TEST_CONNECTION_STRING"] = fixture.ConnectionString };
        if (boundary != null)
        {
            environment["ELSA_ADMISSION_RUNTIME_BOUNDARY"] = boundary;
            environment["ELSA_ADMISSION_RUNTIME_GATE"] = gate!;
        }
        return environment;
    }

    private async Task<int> CountInstancesAsync(string instanceId)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        // EF maps ManagementElsaDbContext.WorkflowInstances to this table in the default
        // selected-provider fixture schema. No second management/runtime provider is built.
        await using var command = new NpgsqlCommand("SELECT count(*)::integer FROM \"WorkflowInstances\" WHERE \"Id\" = @id", connection);
        command.Parameters.AddWithValue("id", instanceId);
        return (int)(await command.ExecuteScalarAsync())!;
    }
}
