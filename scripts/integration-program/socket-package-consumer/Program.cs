using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SocketPackageConsumer;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        string stage = "configuration";
        try
        {
            var options = ConsumerOptions.Parse(args);
            Require.That(!File.Exists(options.Output), "output-already-exists");
            using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(5));
            stage = "socket-vertical";
            var scenario = await Scenarios.RunAsync(options, timeout.Token);
            stage = "report";
            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(x => x != typeof(Program).Assembly && x.GetName().Name?.StartsWith("Elsa", StringComparison.Ordinal) == true)
                .OrderBy(x => x.GetName().Name, StringComparer.Ordinal)
                .Select(x => new AssemblyEvidence(x.GetName().Name!, x.FullName!, x.GetName().Version!.ToString(),
                    x.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "", x.Location, Hash.File(x.Location)))
                .ToArray();
            foreach (var name in ConsumerOptions.RequiredAssemblies)
            {
                Require.That(assemblies.Count(x => x.Name == name) == 1, "required-assembly-not-loaded");
            }
            var process = Process.GetCurrentProcess();
            var report = new ConsumerReport(1, options.SourceRevision, options.CandidateVersion, options.Tfm, options.Feature,
                AppContext.TargetFrameworkName ?? "", RuntimeInformation.FrameworkDescription,
                new(Environment.ProcessId, Hash.Text($"{Environment.ProcessId}:{process.StartTime.ToUniversalTime().Ticks}")),
                assemblies, [scenario]);
            var json = JsonSerializer.Serialize(report, Json.Options);
            Require.NoSecret(json);
            var directory = Path.GetDirectoryName(options.Output)!;
            Directory.CreateDirectory(directory);
            var temporary = Path.Combine(directory, $".{Guid.NewGuid():N}.tmp");
            try
            {
                await File.WriteAllTextAsync(temporary, json, timeout.Token);
                File.Move(temporary, options.Output, false);
            }
            finally
            {
                if (File.Exists(temporary))
                {
                    File.Delete(temporary);
                }
            }
            Console.WriteLine("SOCKET_PACKAGE_CONSUMER_PASS");
            return 0;
        }
        catch (Exception exception)
        {
            // Never render exceptions: provider errors can include connection strings or values.
            var category = exception switch
            {
                ProofFailure failure => "assertion:" + failure.Code,
                OperationCanceledException => "timeout",
                _ => "operation"
            };
            Console.Error.WriteLine($"SOCKET_PACKAGE_CONSUMER_FAIL:{stage}:{category}");
            WriteFailureDiagnostic(exception);
            return 1;
        }
    }

    private static void WriteFailureDiagnostic(Exception exception)
    {
        // Fixed type categories and trusted fixture coordinates only. Never emit
        // exception messages, arbitrary type names, file paths or raw stack traces.
        var category = exception switch
        {
            Npgsql.PostgresException => "postgres",
            Microsoft.EntityFrameworkCore.DbUpdateException => "db-update",
            ArgumentException => "argument",
            InvalidOperationException => "invalid-operation",
            NotSupportedException => "not-supported",
            InvalidCastException => "invalid-cast",
            NullReferenceException => "null-reference",
            TimeoutException => "timeout",
            OperationCanceledException => "cancelled",
            IOException => "io",
            _ => "other"
        };
        var postgres = exception as Npgsql.PostgresException ?? exception.InnerException as Npgsql.PostgresException;
        var sqlState = postgres?.SqlState;
        if (sqlState is null || !Regex.IsMatch(sqlState, "^[0-9A-Z]{5}$"))
        {
            sqlState = "none";
        }
        string[] trustedFiles = ["Program.cs", "ConsumerHost.cs", "ActivitiesAndPolicies.cs", "Migrations.cs", "Scenarios.cs", "LoopbackPeer.cs"];
        var frame = new StackTrace(exception, true).GetFrames()?.FirstOrDefault(x =>
            trustedFiles.Contains(Path.GetFileName(x.GetFileName()), StringComparer.Ordinal) && x.GetFileLineNumber() > 0);
        var file = frame is null ? "none" : Path.GetFileName(frame.GetFileName());
        var line = frame?.GetFileLineNumber() ?? 0;
        Console.Error.WriteLine($"SOCKET_PACKAGE_CONSUMER_DIAGNOSTIC:{category}:{sqlState}:{file}:{line}");
    }
}

internal sealed record ConsumerOptions(string Feature, string Tfm, string Output, string SourceRevision,
    string CandidateVersion, string Connection)
{
    public static readonly string[] RequiredAssemblies =
    [
        "Elsa.Slack.SocketMode", "Elsa.Slack", "Elsa.Connections", "Elsa.Connections.Credentials.Workflows", "Elsa.Connections.Credentials.Persistence.EFCore",
        "Elsa.Connections.Credentials.Persistence.EFCore.PostgreSql", "Elsa.Workflows.Admission",
        "Elsa.Workflows.Admission.Persistence.EFCore", "Elsa.Workflows.Admission.Persistence.EFCore.PostgreSql"
    ];

    public static ConsumerOptions Parse(string[] args)
    {
        Require.That(args.Length == 6, "argument-count");
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var i = 0; i < args.Length; i += 2)
        {
            Require.That(values.TryAdd(args[i], args[i + 1]), "duplicate-argument");
        }
        Require.That(values.Keys.Order().SequenceEqual(new[] { "--feature", "--output", "--tfm" }), "unknown-argument");
        var feature = values["--feature"];
        var tfm = values["--tfm"];
        Require.That(feature is "classic" or "shell", "feature");
        Require.That(tfm is "net8.0" or "net9.0" or "net10.0", "tfm");
        Require.That(AppContext.TargetFrameworkName == $".NETCoreApp,Version=v{tfm[3..]}", "actual-target-framework");
        var output = values["--output"];
        Require.That(Path.IsPathFullyQualified(output), "absolute-output-required");
        var revision = RequiredEnvironment("ELSA_SOCKET_PACKAGE_SOURCE_REVISION");
        Require.That(Regex.IsMatch(revision, "^[0-9a-f]{40}$"), "source-revision");
        var version = RequiredEnvironment("ELSA_SOCKET_PACKAGE_CANDIDATE_VERSION");
        Require.That(Regex.IsMatch(version, "^[0-9A-Za-z.+-]{1,128}$"), "candidate-version");
        return new(feature, tfm, output, revision, version,
            RequiredEnvironment("ELSA_SOCKET_PACKAGE_CONNECTION_STRING"));
    }

    private static string RequiredEnvironment(string name) => Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
        ? value : throw new ProofFailure("missing-environment");
}

internal static class Require
{
    public static void That(bool condition, string code)
    {
        if (!condition)
        {
            throw new ProofFailure(code);
        }
    }

    public static async Task DeniedAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (InvalidOperationException)
        {
            return;
        }
        throw new ProofFailure("owned-operation-not-denied");
    }

    public static void NoSecret(string text) => That(!text.Contains(FixtureConstants.SecretMarker, StringComparison.Ordinal), "secret-marker-observed");
}

internal sealed class ProofFailure(string code) : Exception
{
    public string Code { get; } = code;
}
internal static class Hash
{
    public static string Text(string text) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
    public static string File(string path) => Convert.ToHexString(SHA256.HashData(System.IO.File.ReadAllBytes(path))).ToLowerInvariant();
}
internal static class Json
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { WriteIndented = true };
}
internal sealed record ConsumerReport(int SchemaVersion, string SourceRevision, string CandidateVersion, string Tfm, string Feature,
    string TargetFramework, string FrameworkDescription, ProcessEvidence Process, AssemblyEvidence[] LoadedAssemblies, ScenarioEvidence[] Scenarios);
internal sealed record ProcessEvidence(int Pid, string StartIdentitySha256);
internal sealed record AssemblyEvidence(string Name, string FullName, string Version, string InformationalVersion, string Location, string Sha256);
internal sealed record ScenarioEvidence(string Scenario, string DatabaseIdentitySha256, int ServerVersion,
    Dictionary<string, bool> Assertions, Dictionary<string, int> Counters, Dictionary<string, string> Correlation, OrderEvidence Order, MigrationEvidence[] Migrations, CleanupEvidence Cleanup);
internal sealed record OrderEvidence(long AdmissionCommitted, long AcknowledgementReceived);
internal sealed record CleanupEvidence(bool HostsDisposed, int HostsCreated, int MaximumConcurrentHosts, int ExecutionHostsObserved);
internal sealed record MigrationEvidence(string Context, string Provider, string Schema, string HistoryTable, string[] KnownIds, string[] AppliedIds, string[] HistoryIds,
    bool Reapplied, bool PopulatedPreserved);
