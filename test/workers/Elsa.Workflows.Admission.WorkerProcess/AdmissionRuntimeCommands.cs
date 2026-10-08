using System.Diagnostics;
using System.Text.Json;
using Elsa.Extensions;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Messages;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Admission.WorkerProcess;

internal static class AdmissionRuntimeCommands
{
    public static async Task<int> RunAsync(string[] args)
    {
        try
        {
            var connection = Environment.GetEnvironmentVariable("ELSA_TEST_CONNECTION_STRING") ?? throw new InvalidOperationException("connection_required");
            var outcome = Environment.GetEnvironmentVariable("ELSA_ADMISSION_RUNTIME_OUTCOME") ?? "completed";
            if (outcome is not ("completed" or "suspended" or "faulted"))
            {
                throw new InvalidOperationException("unsupported_fixture_outcome");
            }
            var probe = new AdmissionRuntimeProbe(connection) { Outcome = outcome };
            var boundary = Environment.GetEnvironmentVariable("ELSA_ADMISSION_RUNTIME_BOUNDARY");
            var allowed = Enum.GetNames<AdmissionExecutionBoundary>().Concat(["BeforeInstanceInsert", "ActivityEffect", "FinalCommit", "TrailingWrite"]).ToHashSet(StringComparer.Ordinal);
            if (boundary != null && !allowed.Contains(boundary))
            {
                throw new InvalidOperationException("unsupported_fixture_boundary");
            }
            probe.Boundary = async current =>
            {
                if (current == boundary)
                {
                    Console.WriteLine("BOUNDARY:" + current);
                    await Console.Out.FlushAsync();
                    await AdmissionFileGate.WaitAsync(Environment.GetEnvironmentVariable("ELSA_ADMISSION_RUNTIME_GATE")!);
                }
            };
            await using var services = AdmissionRuntimeHost.CreateServices(connection, probe);
            using var tenant = AdmissionRuntimeHost.EnterTenant(services);
            await services.GetRequiredService<AdmissionHostConfiguration>().ValidateAsync(services);
            await services.GetRequiredService<IRegistriesPopulator>().PopulateAsync();
            var execution = services.GetRequiredService<AdmissionExecutionService>();
            var replayed = false;
            switch (args[0])
            {
                case "runtime-execute":
                    await execution.ExecuteAsync(args[1]);
                    break;
                case "runtime-recover":
                    await execution.RecoverAsync(args[1]);
                    replayed = await execution.ExecuteAsync(args[1]) != null;
                    break;
                case "runtime-resume":
                    var initial = (await services.GetRequiredService<IAdmissionStore>().FindAsync(args[1]))!;
                    var client = await services.GetRequiredService<IWorkflowRuntime>().CreateClientAsync(initial.WorkflowInstanceId);
                    await client.RunInstanceAsync(new RunWorkflowInstanceRequest { BookmarkId = args[2] });
                    break;
                default:
                    throw new InvalidOperationException("unknown_runtime_command");
            }
            var record = (await services.GetRequiredService<IAdmissionStore>().FindAsync(args[1]))!;
            Console.WriteLine("RESULT:" + JsonSerializer.Serialize(new
            {
                processId = Environment.ProcessId, processStartedAt = Process.GetCurrentProcess().StartTime.ToUniversalTime(),
                result = new
                {
                    state = record.State.ToString(), record.AuthorityOutstanding, replayed,
                    activityEffects = await probe.ReadDurableCountAsync("activityEffects"),
                    activityResumes = await probe.ReadDurableCountAsync("activityResumes"),
                    checkpointCount = await probe.ReadDurableCountAsync("CheckpointRecorded")
                }
            }));
            return 0;
        }
        catch
        {
            // The proof stores fixed predicates/counters, never arbitrary exception text.
            Console.Error.WriteLine("admission_runtime_command_failed");
            return 1;
        }
    }
}
