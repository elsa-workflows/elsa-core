using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

using NuGetUpdater.Core;
using NuGetUpdater.Core.Discover;
using NuGetUpdater.Core.Run.ApiModel;

// Temporary, credential-free #8636 diagnostic. Writer smoke mutates only its disposable snapshot; no proposal API.
if (args.Length is not (4 or 5) || args[3] is not ("enumerate" or "tooling" or "retained-probe" or "product-smoke" or "writer-producer" or "writer-smoke"))
{
    return 1;
}
if ((args[3] == "product-smoke") != (args.Length == 5))
{
    return 1;
}

var snapshot = Path.GetFullPath(args[0]);
var output = Path.GetFullPath(args[1]);
var units = JsonSerializer.Deserialize<UnitInput[]>(await File.ReadAllTextAsync(args[2]))!;
if (units.Length != 5 || !units.Select(unit => unit.Id).SequenceEqual(new[] { "core-production", "core-tests", "extensions", "studio", "tooling" }))
{
    return 1;
}

string[] Directories(UnitInput unit) => new Job
{
    Source = new JobSource { Provider = "github", Repo = "elsa-workflows/elsa-core", Directories = unit.Selectors }
}.GetAllDirectories(snapshot).ToArray();

async Task WriteAtomic(string name, object value)
{
    var path = Path.Combine(output, name);
    await File.WriteAllTextAsync(path + ".tmp", JsonSerializer.Serialize(value));
    File.Move(path + ".tmp", path, true);
}

string Relative(string path) => Path.GetRelativePath(snapshot, path).Replace('\\', '/');
var supportedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".sln", ".slnx", ".proj", ".csproj", ".fsproj", ".vbproj" };
if (args[3] == "enumerate")
{
    for (var index = 0; index < units.Length; index++)
    {
        var directories = Directories(units[index]);
        var entries = directories.SelectMany(directory => Directory.EnumerateFiles(Path.Combine(snapshot, directory.TrimStart('/'))))
            .Where(path => supportedExtensions.Contains(Path.GetExtension(path)))
            .Select(Relative).Distinct().OrderBy(path => path, StringComparer.Ordinal).ToArray();
        await WriteAtomic($"enumeration-{index}.json", new { Id = units[index].Id, Directories = directories, EntryPoints = entries });
    }
    return 0;
}

if (args[3] is "writer-producer" or "writer-smoke")
{
    return await WriterSmoke.RunAsync(snapshot, output, args[3]);
}

var workspaces = args[3] switch
{
    "tooling" => Directories(units[^1]),
    "product-smoke" => new[] { "/extensions/src/email/Elsa.Email", "/studio/src/framework/Elsa.Studio.Core" },
    _ => new[] { "/extensions/src/Elsa.Testing.Extensions" }
};
var first = 0;
var count = workspaces.Length;
if (args[3] == "product-smoke")
{
    // Internal fixed-pair index lets Python validate each result before starting the next.
    if (args.Length != 5 || !int.TryParse(args[4], out first) || first is not (0 or 1))
    {
        return 1;
    }
    var unitIndex = first == 0 ? 2 : 3;
    if (!Directories(units[unitIndex]).Contains(workspaces[first], StringComparer.Ordinal))
    {
        return 1;
    }
    count = first + 1;
}
else if (args.Length != 4)
{
    return 1;
}
var returned = true;
for (var index = first; index < count; index++)
{
    var clock = Stopwatch.StartNew();
    await WriteAtomic($"workspace-{index}.call.json", new { Index = index, Workspace = workspaces[index], State = "started", ElapsedSeconds = 0.0 });
    Console.WriteLine($"ELSA8636_WORKSPACE_{index}_STARTED");
    try
    {
        var worker = new DiscoveryWorker("8636-batch-diagnostic", new ExperimentsManager(), new ConsoleLogger());
        // Official worker output and its raw logs remain private; Python publishes only closed fields.
        await worker.RunAsync(snapshot, workspaces[index], Path.Combine(output, $"workspace-{index}.json"));
        await WriteAtomic($"workspace-{index}.call.json", new { Index = index, Workspace = workspaces[index], State = "returned", ElapsedSeconds = clock.Elapsed.TotalSeconds });
        Console.WriteLine($"ELSA8636_WORKSPACE_{index}_RETURNED");
    }
    catch
    {
        returned = false;
        await WriteAtomic($"workspace-{index}.call.json", new { Index = index, Workspace = workspaces[index], State = "threw", ElapsedSeconds = clock.Elapsed.TotalSeconds });
        Console.WriteLine($"ELSA8636_WORKSPACE_{index}_THREW");
    }
}
return returned ? 0 : 1;

internal sealed record UnitInput(string Id, string[] Selectors);

// #8636: remove with the temporary native diagnostic after its evidence is accepted.
internal static class WriterSmoke
{
    internal const string Project = "studio/src/modules/Elsa.Studio.Authentication.Abstractions/Elsa.Studio.Authentication.Abstractions.csproj";
    internal const string Package = "Microsoft.AspNetCore.SignalR.Client";
    internal const string TargetVersion = "10.0.12";
    internal const string Central = "studio/src/Directory.Packages.props";

    internal static async Task<int> RunAsync(string snapshot, string output, string mode)
    {
        // The production registration call may temporarily rename global.json.
        // Keep writer registration at currentDirectory == rootDirectory; cold producer has
        // an upstream relative-root ancestor walk, guarded both here and by Python.
        if (Path.GetFullPath(Environment.CurrentDirectory) != snapshot)
        {
            return 1;
        }
        for (var parent = Directory.GetParent(snapshot); parent is not null; parent = parent.Parent)
        {
            if (File.Exists(Path.Combine(parent.FullName, "global.json")))
            {
                return 1;
            }
        }
        var logger = new WriterLogger(snapshot, output);
        if (mode == "writer-producer")
        {
            await new DiscoveryWorker("8636-writer-producer", new ExperimentsManager(), logger)
                .RunAsync(snapshot, Path.GetDirectoryName(Project)!, Path.Combine(output, "workspace-2.json"));
            return 0;
        }

        var request = JsonSerializer.Deserialize<WriterRequest>(await File.ReadAllTextAsync(Path.Combine(output, "writer-request.json")))!;
        if (request.Project != Project || request.Package != Package || request.NewVersion != TargetVersion
            || request.OldVersion is not ("8.0.24" or "9.0.13" or "10.0.3"))
        {
            return 1;
        }
        // Recheck the admitted old version against the producer, without another native call.
        using var producer = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(output, "workspace-2.json")));
        var root = producer.RootElement;
        var candidates = root.GetProperty("Projects").EnumerateArray().Where(row =>
            logger.Relative(Path.Combine(snapshot, root.GetProperty("Path").GetString()!, row.GetProperty("FilePath").GetString()!)) == Project).ToArray();
        if (!root.GetProperty("IsSuccess").GetBoolean() || candidates.Length != 1
            || !candidates[0].GetProperty("IsSuccess").GetBoolean())
        {
            return 1;
        }
        var dependencies = candidates[0].GetProperty("Dependencies").EnumerateArray()
            .Where(row => row.GetProperty("Name").GetString() == Package).ToArray();
        if (dependencies.Length != 1 || dependencies[0].GetProperty("Version").GetString() != request.OldVersion)
        {
            return 1;
        }

        // UpdaterWorker owns MSBuild registration and its real writer construction.
        var project = new FileInfo(Path.Combine(snapshot, Project));
        var worker = new UpdaterWorker("8636-writer", new ExperimentsManager(), logger);
        logger.Record("request", "started");
        try
        {
            var result = await worker.RunAsync(snapshot, project.FullName, Package, request.OldVersion, TargetVersion, true);
            var operations = result.UpdateOperations;
            // Raw native operations remain private; Python projects a closed summary.
            await File.WriteAllTextAsync(Path.Combine(output, "writer-operations.json"), JsonSerializer.Serialize(
                operations.Select(operation => new
                {
                    operation.Type,
                    operation.DependencyName,
                    OldVersion = operation.OldVersion?.ToString(),
                    NewVersion = operation.NewVersion.ToString(),
                    operation.UpdatedFiles
                })));
            logger.Record("request", "returned");
            return 0;
        }
        catch
        {
            logger.Record("request", "threw");
            throw;
        }
    }
}

internal sealed record WriterRequest(string Project, string Package, string OldVersion, string NewVersion);

internal sealed class WriterLogger(string snapshot, string output) : ILogger
{
    private readonly List<object> _events = [];
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly HashSet<string> _tracked = JsonSerializer.Deserialize<string[]>(File.ReadAllText(Path.Combine(output, "writer-tracked.json")))!.ToHashSet(StringComparer.Ordinal);
    private int _attempt;
    private int _discoveries;
    internal bool Dropped { get; private set; }
    internal string? AttemptProject { get; private set; }

    internal string? Relative(string path)
    {
        var relative = Path.GetRelativePath(snapshot, Path.GetFullPath(path)).Replace('\\', '/');
        return _tracked.Contains(relative) ? relative : null;
    }

    internal void Record(string kind, string state, int ordinal = 0, string? workspace = null)
    {
        if (_events.Count >= 512)
        {
            Dropped = true;
        }
        else
        {
            var central = Path.Combine(snapshot, WriterSmoke.Central);
            var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(central))).ToLowerInvariant();
            _events.Add(new
            {
                Sequence = _events.Count,
                Kind = kind,
                State = state,
                Ordinal = ordinal,
                Tfm = (string?)null,
                Attempt = _attempt,
                AttemptProject,
                Workspace = workspace,
                ElapsedSeconds = _clock.Elapsed.TotalSeconds,
                CentralHash = hash
            });
        }
        var path = Path.Combine(output, "writer-events.json");
        File.WriteAllText(path + ".tmp", JsonSerializer.Serialize(new { Events = _events, Dropped }));
        File.Move(path + ".tmp", path, true);
    }

    public void LogRaw(string message)
    {
        // Preserve raw logs privately; only exact pinned tokens become events.
        Console.WriteLine(message);
        var match = Regex.Match(message, @"^\d{4}/\d{2}/\d{2} \d{2}:\d{2}:\d{2} INFO Attempting to update Microsoft\.AspNetCore\.SignalR\.Client for (.+)$");
        if (match.Success)
        {
            var relative = match.Groups[1].Value.TrimStart('/');
            if (_tracked.Contains(relative))
            {
                _attempt++;
                AttemptProject = relative;
                Record("attempt", "started");
            }
        }
        else if (Regex.IsMatch(message, @"^\d{4}/\d{2}/\d{2} \d{2}:\d{2}:\d{2} INFO Discovering build files in workspace \[.+\]\.$"))
        {
            _discoveries++;
            Record("discovery", "started", _discoveries);
        }
        else if (Regex.IsMatch(message, @"^\d{4}/\d{2}/\d{2} \d{2}:\d{2}:\d{2} INFO Discovery complete\.$"))
        {
            Record("discovery", "returned", _discoveries);
        }
        else if (Regex.IsMatch(message, @"^\d{4}/\d{2}/\d{2} \d{2}:\d{2}:\d{2} WARN   Unable to find dependency after discovery rerun\.$"))
        {
            Record("missing_dependency", "observed");
        }
        else if (Regex.IsMatch(message, @"^\d{4}/\d{2}/\d{2} \d{2}:\d{2}:\d{2} WARN Dependency Microsoft\.AspNetCore\.SignalR\.Client not found in final project discovery\.$"))
        {
            Record("final_validation", "missing_requested_dependency", _discoveries);
        }
        else
        {
            match = Regex.Match(message, @"^\d{4}/\d{2}/\d{2} \d{2}:\d{2}:\d{2} WARN Unable to find final project discovery for project (.+)\.$");
            if (match.Success && match.Groups[1].Value == Path.Combine(snapshot, WriterSmoke.Project))
            {
                Record("final_validation", "missing_project", _discoveries);
            }
            else
            {
                match = Regex.Match(message, @"^\d{4}/\d{2}/\d{2} \d{2}:\d{2}:\d{2} WARN Final dependency version for Microsoft\.AspNetCore\.SignalR\.Client is ([0-9][0-9A-Za-z.+-]{0,100}), expected " + Regex.Escape(WriterSmoke.TargetVersion) + @"\.$");
                if (match.Success && NuGet.Versioning.NuGetVersion.TryParse(match.Groups[1].Value, out var observedVersion)
                    && observedVersion != NuGet.Versioning.NuGetVersion.Parse(WriterSmoke.TargetVersion))
                {
                    // Only the closed reason is emitted; native version/message text stays private.
                    Record("final_validation", "version_mismatch", _discoveries);
                }
            }
        }
    }
}
