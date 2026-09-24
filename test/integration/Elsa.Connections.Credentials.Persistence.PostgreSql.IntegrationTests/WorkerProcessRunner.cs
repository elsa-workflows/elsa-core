using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Channels;
using Elsa.Connections.Credentials.WorkerProcess;

namespace Elsa.Connections.Credentials.Persistence.PostgreSql.IntegrationTests;

internal sealed class WorkerProcessRunner
{
    private readonly string _workerAssemblyPath = typeof(WorkerCommandHost).Assembly.Location;

    public ProcessRun Start(IEnumerable<string> arguments, IReadOnlyDictionary<string, string> environment)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add(_workerAssemblyPath);
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }
        foreach (var pair in environment)
        {
            startInfo.Environment[pair.Key] = pair.Value;
        }

        var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        if (!process.Start())
        {
            throw new InvalidOperationException("worker_process_start_failed");
        }

        var result = new ProcessRun(process);
        result.StartReaders();
        return result;
    }

    public async Task<ProcessRunResult> RunAsync(IEnumerable<string> arguments, IReadOnlyDictionary<string, string> environment)
    {
        await using var process = Start(arguments, environment);
        return await process.CompleteAsync();
    }
}

internal sealed class ProcessRun(Process process) : IAsyncDisposable
{
    private readonly Channel<string> _stdoutChannel = Channel.CreateUnbounded<string>();
    private readonly ConcurrentQueue<string> _stdout = new();
    private readonly ConcurrentQueue<string> _stderr = new();
    private Task? _stdoutReader;
    private Task? _stderrReader;

    public void StartReaders()
    {
        _stdoutReader = ReadLinesAsync(process.StandardOutput, _stdout, _stdoutChannel.Writer);
        _stderrReader = ReadLinesAsync(process.StandardError, _stderr, null);
    }

    public async Task WaitForLineAsync(string expectedLine, TimeSpan? timeout = null)
    {
        using var cancellation = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(30));
        try
        {
            while (await _stdoutChannel.Reader.WaitToReadAsync(cancellation.Token))
            {
                while (_stdoutChannel.Reader.TryRead(out var line))
                {
                    if (line == expectedLine)
                        return;
                }
            }
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            // Print only test-owned page counts, never the worker's arbitrary output or provider payloads.
            var pageCounts = _stdout
                .Where(line => line.StartsWith("DUE_PAGE:", StringComparison.Ordinal) &&
                               int.TryParse(line["DUE_PAGE:".Length..], out _))
                .GroupBy(line => line)
                .Select(group => $"{group.Key} x{group.Count()}");
            throw new TimeoutException($"worker_expected_boundary_missing: {expectedLine}; {string.Join(", ", pageCounts)}");
        }

        throw new InvalidOperationException("worker_expected_boundary_missing");
    }

    public async Task<ProcessRunResult> CompleteAsync(TimeSpan? timeout = null)
    {
        using var cancellation = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(45));
        try
        {
            await process.WaitForExitAsync(cancellation.Token);
        }
        catch (OperationCanceledException)
        {
            await TerminateAsync();
            throw new TimeoutException("worker_process_timed_out");
        }

        await Task.WhenAll(_stdoutReader!, _stderrReader!);
        return new ProcessRunResult(process.Id, process.ExitCode, _stdout.ToArray(), _stderr.ToArray());
    }

    public async Task<ProcessRunResult> TerminateAsync()
    {
        if (!process.HasExited)
        {
            process.Kill(entireProcessTree: true);
        }

        await process.WaitForExitAsync();
        await Task.WhenAll(_stdoutReader!, _stderrReader!);
        return new ProcessRunResult(process.Id, process.ExitCode, _stdout.ToArray(), _stderr.ToArray());
    }

    public async ValueTask DisposeAsync()
    {
        if (!process.HasExited)
        {
            await TerminateAsync();
        }

        process.Dispose();
    }

    private static async Task ReadLinesAsync(StreamReader reader, ConcurrentQueue<string> output, ChannelWriter<string>? channel)
    {
        try
        {
            while (await reader.ReadLineAsync() is { } line)
            {
                output.Enqueue(line);
                channel?.TryWrite(line);
            }
        }
        finally
        {
            channel?.TryComplete();
        }
    }
}

internal sealed record ProcessRunResult(int ProcessId, int ExitCode, IReadOnlyList<string> StandardOutput, IReadOnlyList<string> StandardError)
{
    public JsonElement ReadResult()
    {
        var line = StandardOutput.LastOrDefault(value => value.StartsWith("RESULT:", StringComparison.Ordinal));
        if (line is null)
        {
            throw new InvalidOperationException("worker_result_missing");
        }

        using var document = JsonDocument.Parse(line["RESULT:".Length..]);
        return document.RootElement.Clone();
    }

    public string CapturedOutput => string.Join('\n', StandardOutput.Concat(StandardError));
}

internal static class ProcessTestEnvironment
{
    public const string TenantId = "tenant-process-test";
    public const string EnvironmentId = "integration";
    public const string AccessMarker = "access-never-log-8a6f";
    public const string RefreshMarker = "refresh-never-log-1f92";
    public const string RotatedAccessTokenPrefix = "access-rotated-";
    public const string RotatedRefreshTokenPrefix = "refresh-rotated-";

    public static readonly DateTimeOffset InitialTime = new(2026, 9, 24, 12, 0, 0, TimeSpan.Zero);
    public static readonly byte[] EncryptionKey = Enumerable.Range(1, 32).Select(value => (byte)value).ToArray();

    public static Dictionary<string, string> Create(string connectionString, string providerAddress)
    {
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["ELSA_TEST_CONNECTION_STRING"] = connectionString,
            ["ELSA_TEST_TENANT_ID"] = TenantId,
            ["ELSA_TEST_ENVIRONMENT_ID"] = EnvironmentId,
            ["ELSA_TEST_PROVIDER_ADDRESS"] = providerAddress,
            ["ELSA_TEST_ENCRYPTION_KEY_BASE64"] = Convert.ToBase64String(EncryptionKey),
            ["ELSA_TEST_NOW_UTC"] = InitialTime.ToString("O"),
            ["ELSA_TEST_INITIAL_ACCESS_TOKEN"] = AccessMarker,
            ["ELSA_TEST_INITIAL_REFRESH_TOKEN"] = RefreshMarker,
            ["ELSA_TEST_PERMISSIONS"] = "connections.manage,connections.use",
            ["ELSA_TEST_ALLOW_BACKGROUND"] = "true",
            ["ELSA_TEST_ALLOW_BINDING_USE"] = "true"
        };
    }

    public static Dictionary<string, string> With(IReadOnlyDictionary<string, string> source, params (string Key, string Value)[] values)
    {
        var result = source.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        foreach (var (key, value) in values)
        {
            result[key] = value;
        }

        return result;
    }
}
