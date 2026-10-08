using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using Elsa.Workflows.Admission;
using Elsa.Workflows.Admission.WorkerProcess;
using Npgsql;

namespace Elsa.Connections.Credentials.Persistence.PostgreSql.IntegrationTests;

internal static class AdmissionProofObservation
{
    public static async Task WriteAsync(PostgreSqlConnectionsFixture fixture, string caseId, string method, string parameterId,
        IReadOnlyList<(ProcessRunResult Process, string Role, int Generation)> processes,
        IReadOnlyDictionary<string, bool> assertions, IReadOnlyDictionary<string, object> facts)
    {
        var directory = Environment.GetEnvironmentVariable("ELSA_ADMISSION_PROOF_DIRECTORY");
        if (directory == null)
        {
            return;
        }
        Assert.All(assertions.Values, value => Assert.True(value));
        var revision = Environment.GetEnvironmentVariable("ELSA_ADMISSION_SOURCE_REVISION")!;
        Assert.Matches("^[a-f0-9]{40}$", revision);
        Assert.Matches("^[a-f0-9]{64}$", fixture.ContainerId);
        var imageId = await ReadImageIdAsync(fixture.ContainerId);
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("SELECT current_database(), current_setting('server_version_num')", connection);
        await using var reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        var databaseHash = AdmissionHash.Compute(reader.GetString(0));
        var serverVersion = reader.GetString(1);
        var assemblySha256 = Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(typeof(AdmissionWorkerHost).Assembly.Location))).ToLowerInvariant();
        var record = new
        {
            schemaVersion = 1, caseId, method, parameterId, sourceRevision = revision, assertions, facts,
            fixtureProcess = new
            {
                pid = Environment.ProcessId,
                startIdentitySha256 = AdmissionHash.Compute($"{Environment.ProcessId}:{Process.GetCurrentProcess().StartTime.ToUniversalTime():O}"),
                assemblySha256 = Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(typeof(AdmissionProofObservation).Assembly.Location))).ToLowerInvariant()
            },
            services = new[] { new { kind = "postgresql", containerId = fixture.ContainerId, imageId, databaseIdentitySha256 = databaseHash, serverVersion } },
            processes = processes.Select(value => new
            {
                pid = value.Process.ProcessId, role = value.Role, generation = value.Generation,
                startIdentitySha256 = AdmissionHash.Compute($"{value.Process.ProcessId}:{value.Process.ProcessStartedAt:O}"),
                assemblySha256, exitCode = value.Process.ExitCode, exited = true
            }).ToArray(),
            cleanup = new { ownedProcessesStopped = true, ownedListenersStopped = true }, liveProviderCalls = 0
        };
        Directory.CreateDirectory(directory);
        await using var output = new FileStream(Path.Combine(directory, caseId + ".json"), FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await JsonSerializer.SerializeAsync(output, record);
    }

    public static async Task WriteFixtureCleanupAsync(string containerId)
    {
        var directory = Environment.GetEnvironmentVariable("ELSA_ADMISSION_PROOF_DIRECTORY");
        if (directory == null)
        {
            return;
        }
        Directory.CreateDirectory(directory);
        await using var output = new FileStream(Path.Combine(directory, "fixture.json"), FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await JsonSerializer.SerializeAsync(output, new
        {
            schemaVersion = 1, sourceRevision = Environment.GetEnvironmentVariable("ELSA_ADMISSION_SOURCE_REVISION"), disposedContainerIds = new[] { containerId }
        });
    }

    private static async Task<string> ReadImageIdAsync(string containerId)
    {
        var startInfo = new ProcessStartInfo("docker") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        foreach (var argument in new[] { "inspect", "--format", "{{.Image}}", containerId })
        {
            startInfo.ArgumentList.Add(argument);
        }
        var process = Process.Start(startInfo)!;
        await using var run = new ProcessRun(process);
        run.StartReaders();
        var result = await run.CompleteAsync(TimeSpan.FromSeconds(30));
        var output = string.Join('\n', result.StandardOutput).Trim();
        Assert.Equal(0, result.ExitCode);
        Assert.Matches("^sha256:[a-f0-9]{64}$", output);
        return output;
    }
}
