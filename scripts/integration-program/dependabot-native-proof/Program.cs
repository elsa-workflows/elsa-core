using System.Diagnostics;
using System.Text.Json;

using NuGetUpdater.Core;
using NuGetUpdater.Core.Discover;
using NuGetUpdater.Core.Run.ApiModel;

// Temporary, credential-free #8636 diagnostic. Never invokes the updater or its proposal API.
if (args.Length != 4 || args[3] is not ("enumerate" or "tooling" or "retained-probe"))
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

var workspaces = args[3] == "tooling" ? Directories(units[^1]) : new[] { "/extensions/src/Elsa.Testing.Extensions" };
var returned = true;
for (var index = 0; index < workspaces.Length; index++)
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
